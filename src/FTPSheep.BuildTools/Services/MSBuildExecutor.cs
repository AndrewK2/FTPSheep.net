using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FTPSheep.BuildTools.Models;

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// Executes MSBuild operations and captures build output.
/// </summary>
public class MsBuildExecutor {
    private readonly MsBuildWrapper wrapper;
    private static readonly Regex errorPattern = new(@"error\s+[A-Z]+\d+:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex warningPattern = new(@"warning\s+[A-Z]+\d+:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="MsBuildExecutor"/> class.
    /// </summary>
    /// <param name="wrapper">The MSBuild wrapper for building arguments.</param>
    public MsBuildExecutor(MsBuildWrapper? wrapper = null) {
        this.wrapper = wrapper ?? new MsBuildWrapper();
    }

    /// <summary>
    /// Builds a project using MSBuild.
    /// </summary>
    /// <param name="options">The MSBuild options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> BuildAsync(MsBuildOptions options, CancellationToken cancellationToken = default) {
        if(!options.Targets.Contains("Build")) {
            options.Targets.Add("Build");
        }

        return await ExecuteAsync(options, cancellationToken);
    }

    /// <summary>
    /// Publishes a project using MSBuild.
    /// </summary>
    /// <param name="options">The MSBuild options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> PublishAsync(MsBuildOptions options, CancellationToken cancellationToken = default) {
        if(!options.Targets.Contains("Publish")) {
            options.Targets.Add("Publish");
        }

        var result = await ExecuteAsync(options, cancellationToken);

        // Set output path if publish was successful
        if(result.Success && !string.IsNullOrWhiteSpace(options.OutputPath)) {
            result.OutputPath = options.OutputPath;
        }

        return result;
    }

    /// <summary>
    /// Cleans a project using MSBuild.
    /// </summary>
    /// <param name="options">The MSBuild options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> CleanAsync(MsBuildOptions options, CancellationToken cancellationToken = default) {
        options.Targets.Clear();
        options.Targets.Add("Clean");
        options.RestorePackages = false;

        return await ExecuteAsync(options, cancellationToken);
    }

    /// <summary>
    /// Rebuilds a project using MSBuild.
    /// </summary>
    /// <param name="options">The MSBuild options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> RebuildAsync(MsBuildOptions options, CancellationToken cancellationToken = default) {
        options.Targets.Clear();
        options.Targets.Add("Rebuild");

        return await ExecuteAsync(options, cancellationToken);
    }

    /// <summary>
    /// Executes MSBuild with the specified options.
    /// </summary>
    /// <param name="options">The MSBuild options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    private async Task<BuildResult> ExecuteAsync(MsBuildOptions options, CancellationToken cancellationToken) {
        var msbuildPath = wrapper.GetMsBuildPath();
        var arguments = wrapper.BuildArguments(options);

        var startTime = DateTime.UtcNow;
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var processStartInfo = new ProcessStartInfo {
            FileName = msbuildPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(options.ProjectPath) ?? Directory.GetCurrentDirectory()
        };

        using var process = new Process { StartInfo = processStartInfo };

        // Capture output and error streams
        process.OutputDataReceived += (sender, e) => {
            if(e.Data != null) {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) => {
            if(e.Data != null) {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for the process to exit or cancellation
        await WaitForExitAsync(process, cancellationToken);

        var duration = DateTime.UtcNow - startTime;
        var output = outputBuilder.ToString();
        var errorOutput = errorBuilder.ToString();

        // Parse errors and warnings from output
        var errors = ParseErrors(output + errorOutput);
        var warnings = ParseWarnings(output);

        if(process.ExitCode == 0 && errors.Count == 0) {
            return BuildResult.Successful(output, duration, options.OutputPath);
        } else {
            return BuildResult.Failed(process.ExitCode, output, errorOutput, errors, duration);
        }
    }

    /// <summary>
    /// Waits for a process to exit asynchronously.
    /// </summary>
    private static async Task WaitForExitAsync(Process process, CancellationToken cancellationToken) {
        while(!process.HasExited) {
            if(cancellationToken.IsCancellationRequested) {
                try {
                    process.Kill();
                } catch {
                    // Process may have already exited
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    /// <summary>
    /// Parses error messages from MSBuild output.
    /// </summary>
    private static List<string> ParseErrors(string output) {
        var errors = new List<string>();
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach(var line in lines) {
            if(errorPattern.IsMatch(line)) {
                errors.Add(line.Trim());
            }
        }

        return errors;
    }

    /// <summary>
    /// Parses warning messages from MSBuild output.
    /// </summary>
    private static List<string> ParseWarnings(string output) {
        var warnings = new List<string>();
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach(var line in lines) {
            if(warningPattern.IsMatch(line)) {
                warnings.Add(line.Trim());
            }
        }

        return warnings;
    }
}
