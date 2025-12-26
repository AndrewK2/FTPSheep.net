using System.Collections.Concurrent;
using FTPSheep.Protocols.Interfaces;
using FTPSheep.Protocols.Models;

namespace FTPSheep.Protocols.Services;

/// <summary>
/// Engine for concurrent file uploads with progress tracking and throttling.
/// </summary>
public class ConcurrentUploadEngine : IDisposable {
    private readonly FtpConnectionConfig config;
    private readonly FtpClientFactory ftpClientFactory;
    private readonly int maxConcurrency;
    private readonly int maxRetries;
    private readonly ConcurrentBag<IFtpClient> clientPool;
    private readonly SemaphoreSlim connectionSemaphore;
    private bool disposed;

    // Progress tracking
    private int totalFiles;
    private int completedFiles;
    private int successfulUploads;
    private int failedUploads;
    private long totalBytes;
    private long uploadedBytes;
    private DateTime startTime;
    private readonly object progressLock = new();

    /// <summary>
    /// Event raised when upload progress is updated.
    /// </summary>
    public event EventHandler<UploadProgress>? ProgressUpdated;

    /// <summary>
    /// Event raised when a file upload completes (success or failure).
    /// </summary>
    public event EventHandler<UploadResult>? FileUploaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentUploadEngine"/> class.
    /// </summary>
    /// <param name="config">The FTP connection configuration.</param>
    /// <param name="ftpClientFactory"></param>
    /// <param name="maxConcurrency">Maximum number of concurrent uploads (default 4).</param>
    /// <param name="maxRetries">Maximum number of retries per file (default 3).</param>
    public ConcurrentUploadEngine(FtpConnectionConfig config, FtpClientFactory ftpClientFactory, int maxConcurrency = 4, int maxRetries = 3) {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.ftpClientFactory = ftpClientFactory ?? throw new ArgumentNullException(nameof(ftpClientFactory));

        if(maxConcurrency is < 1 or > 20) {
            throw new ArgumentException("Max concurrency must be between 1 and 20.", nameof(maxConcurrency));
        }
        if(maxRetries is < 0 or > 10) {
            throw new ArgumentException("Max retries must be between 0 and 10.", nameof(maxRetries));
        }

        this.maxConcurrency = maxConcurrency;
        this.maxRetries = maxRetries;
        clientPool = new ConcurrentBag<IFtpClient>();
        connectionSemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    /// <summary>
    /// Uploads multiple files concurrently.
    /// </summary>
    /// <param name="tasks">The upload tasks to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of upload results.</returns>
    public async Task<IReadOnlyCollection<UploadResult>> UploadFilesAsync(
        IEnumerable<UploadTask> tasks,
        CancellationToken cancellationToken = default) {
        var taskList = tasks.ToList();
        if(taskList.Count == 0) {
            return Array.Empty<UploadResult>();
        }

        // Initialize progress tracking
        InitializeProgress(taskList);

        // Create task queue ordered by priority then size (small files first)
        var taskQueue = new ConcurrentQueue<UploadTask>(
            taskList.OrderBy(t => t.Priority)
                   .ThenBy(t => t.FileSize));

        var results = new ConcurrentBag<UploadResult>();

        // Create worker tasks for concurrent processing
        var workers = Enumerable.Range(0, maxConcurrency)
            .Select(_ => ProcessUploadQueueAsync(taskQueue, results, cancellationToken))
            .ToList();

        // Wait for all workers to complete
        await Task.WhenAll(workers);

        return results.ToList();
    }

    /// <summary>
    /// Worker task that processes uploads from the queue.
    /// </summary>
    private async Task ProcessUploadQueueAsync(
        ConcurrentQueue<UploadTask> taskQueue,
        ConcurrentBag<UploadResult> results,
        CancellationToken cancellationToken) {
        // Get or create FTP client for this worker
        var client = await GetOrCreateClientAsync(cancellationToken);

        try {
            while(!taskQueue.IsEmpty && !cancellationToken.IsCancellationRequested) {
                if(!taskQueue.TryDequeue(out var task)) {
                    continue;
                }

                // Upload file with retry logic
                var result = await UploadWithRetryAsync(client, task, cancellationToken);

                // Store result and update progress
                results.Add(result);
                UpdateProgress(result);

                // Raise event
                FileUploaded?.Invoke(this, result);
            }
        } finally {
            // Return client to pool
            ReturnClientToPool(client);
        }
    }

    /// <summary>
    /// Uploads a single file with retry logic.
    /// </summary>
    private async Task<UploadResult> UploadWithRetryAsync(
        IFtpClient client,
        UploadTask task,
        CancellationToken cancellationToken) {
        var startTime = DateTime.UtcNow;
        var attempts = 0;
        Exception? lastException = null;

        while(attempts <= maxRetries && !cancellationToken.IsCancellationRequested) {
            attempts++;

            try {
                var status = await client.UploadFileAsync(
                    task.LocalPath,
                    task.RemotePath,
                    task.Overwrite,
                    task.CreateRemoteDir,
                    cancellationToken);

                var completedTime = DateTime.UtcNow;

                return UploadResult.FromSuccess(task, status, startTime, completedTime, attempts - 1);
            } catch(Exception ex) {
                lastException = ex;

                // If this is not the last retry, wait before retrying
                if(attempts <= maxRetries) {
                    // Exponential backoff: 1s, 2s, 4s...
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts - 1));
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        // All retries exhausted
        var endTime = DateTime.UtcNow;
        return UploadResult.FromFailure(task, lastException!, startTime, endTime, attempts - 1);
    }

    /// <summary>
    /// Gets or creates an FTP client from the pool.
    /// </summary>
    private async Task<IFtpClient> GetOrCreateClientAsync(CancellationToken cancellationToken) {
        // Wait for available slot
        await connectionSemaphore.WaitAsync(cancellationToken);

        // Try to get existing client from pool
        if(clientPool.TryTake(out var client)) {
            if(client.IsConnected) {
                return client;
            }

            // Client disconnected, dispose and create new one
            client.Dispose();
        }

        // Create and connect new client using factory
        var newClient = ftpClientFactory.CreateClient(config);
        await newClient.ConnectAsync(cancellationToken);

        return newClient;
    }

    /// <summary>
    /// Returns an FTP client to the pool.
    /// </summary>
    private void ReturnClientToPool(IFtpClient client) {
        if(client.IsConnected) {
            clientPool.Add(client);
        } else {
            client.Dispose();
        }

        connectionSemaphore.Release();
    }

    /// <summary>
    /// Initializes progress tracking.
    /// </summary>
    private void InitializeProgress(List<UploadTask> tasks) {
        lock(progressLock) {
            totalFiles = tasks.Count;
            totalBytes = tasks.Sum(t => t.FileSize);
            completedFiles = 0;
            successfulUploads = 0;
            failedUploads = 0;
            uploadedBytes = 0;
            startTime = DateTime.UtcNow;
        }

        // Raise initial progress
        RaiseProgressUpdated();
    }

    /// <summary>
    /// Updates progress after a file upload completes.
    /// </summary>
    private void UpdateProgress(UploadResult result) {
        lock(progressLock) {
            completedFiles++;
            uploadedBytes += result.Task.FileSize;

            if(result.Success) {
                successfulUploads++;
            } else {
                failedUploads++;
            }
        }

        // Raise progress update
        RaiseProgressUpdated();
    }

    /// <summary>
    /// Raises the ProgressUpdated event.
    /// </summary>
    private void RaiseProgressUpdated() {
        lock(progressLock) {
            var elapsed = DateTime.UtcNow - startTime;
            var averageBytesPerSecond = elapsed.TotalSeconds > 0
                ? uploadedBytes / elapsed.TotalSeconds
                : 0;

            var remainingBytes = totalBytes - uploadedBytes;
            var estimatedTimeRemaining = averageBytesPerSecond > 0
                ? TimeSpan.FromSeconds(remainingBytes / averageBytesPerSecond)
                : (TimeSpan?)null;

            // Calculate current speed (based on recent uploads)
            var currentBytesPerSecond = averageBytesPerSecond; // Simplified - could use moving average

            var activeUploads = maxConcurrency - connectionSemaphore.CurrentCount;
            var pendingFiles = totalFiles - completedFiles - activeUploads;

            var progress = new UploadProgress {
                TotalFiles = totalFiles,
                CompletedFiles = completedFiles,
                ActiveUploads = activeUploads,
                PendingFiles = Math.Max(0, pendingFiles),
                SuccessfulUploads = successfulUploads,
                FailedUploads = failedUploads,
                TotalBytes = totalBytes,
                UploadedBytes = uploadedBytes,
                BytesPerSecond = currentBytesPerSecond,
                AverageBytesPerSecond = averageBytesPerSecond,
                EstimatedTimeRemaining = estimatedTimeRemaining,
                StartedAt = startTime
            };

            ProgressUpdated?.Invoke(this, progress);
        }
    }

    /// <summary>
    /// Disposes all FTP clients in the pool.
    /// </summary>
    public void Dispose() {
        if(!disposed) {
            while(clientPool.TryTake(out var client)) {
                client.Dispose();
            }

            connectionSemaphore.Dispose();
            disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
