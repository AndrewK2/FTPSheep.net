using FTPSheep.Core.Models;

namespace FTPSheep.Core.Interfaces;

/// <summary>
/// Defines the contract for managing deployment history.
/// </summary>
public interface IDeploymentHistoryService
{
    /// <summary>
    /// Adds a deployment entry to the history.
    /// </summary>
    /// <param name="entry">The deployment history entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddEntryAsync(DeploymentHistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent deployment history entries.
    /// </summary>
    /// <param name="count">The maximum number of entries to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of deployment history entries, ordered by timestamp descending.</returns>
    Task<List<DeploymentHistoryEntry>> GetRecentEntriesAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployment history entries for a specific profile.
    /// </summary>
    /// <param name="profileName">The profile name.</param>
    /// <param name="count">The maximum number of entries to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of deployment history entries for the profile.</returns>
    Task<List<DeploymentHistoryEntry>> GetProfileEntriesAsync(
        string profileName,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all deployment history entries within a date range.
    /// </summary>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of deployment history entries within the date range.</returns>
    Task<List<DeploymentHistoryEntry>> GetEntriesByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all deployment history.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);
}
