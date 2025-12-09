using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FTPSheep.BuildTools.Models;

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// Executes dotnet CLI operations and captures build output.
/// </summary>
public class DotnetCliExecutor {
    private readonly BuildToolLocator _toolLocator;
    private static readonly Regex ErrorPattern = new(@"error\s+[A-Z]+\d+:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WarningPattern = new(@"warning\s+[A-Z]+\d+:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetCliExecutor"/> class.
    /// </summary>
    /// <param name="toolLocator">The build tool locator.</param>
    public DotnetCliExecutor(BuildToolLocator? toolLocator = null) {
        _toolLocator = toolLocator ?? new BuildToolLocator();
    }

    /// <summary>
    /// Builds a project using dotnet CLI.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="outputPath">Optional output path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> BuildAsync(
        string projectPath,
        string configuration = "Release",
        string? outputPath = null,
        CancellationToken cancellationToken = default) {
        var args = new StringBuilder($"build \"{projectPath}\"");
        args.Append($" --configuration {configuration}");

        if(!string.IsNullOrWhiteSpace(outputPath)) {
            args.Append($" --output \"{outputPath}\"");
        }

        args.Append(" --nologo");

        return await ExecuteAsync(args.ToString(), projectPath, outputPath, cancellationToken);
    }

    /// <summary>
    /// Publishes a project using dotnet CLI.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="outputPath">The output path for published files.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="runtime">Optional runtime identifier (win-x64, linux-x64, etc.).</param>
    /// <param name="selfContained">Whether to publish as self-contained.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> PublishAsync(
        string projectPath,
        string outputPath,
        string configuration = "Release",
        string? runtime = null,
        bool? selfContained = null,
        CancellationToken cancellationToken = default) {
        var args = new StringBuilder($"publish \"{projectPath}\"");
        args.Append($" --configuration {configuration}");
        args.Append($" --output \"{outputPath}\"");

        if(!string.IsNullOrWhiteSpace(runtime)) {
            args.Append($" --runtime {runtime}");
        }

        if(selfContained.HasValue) {
            args.Append($" --self-contained {selfContained.Value.ToString().ToLowerInvariant()}");
        }

        args.Append(" --nologo");

        return await ExecuteAsync(args.ToString(), projectPath, outputPath, cancellationToken);
    }

    /// <summary>
    /// Cleans a project using dotnet CLI.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration (Debug, Release).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> CleanAsync(
        string projectPath,
        string configuration = "Release",
        CancellationToken cancellationToken = default) {
        var args = $"clean \"{projectPath}\" --configuration {configuration} --nologo";
        return await ExecuteAsync(args, projectPath, null, cancellationToken);
    }

    /// <summary>
    /// Restores NuGet packages for a project using dotnet CLI.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    public async Task<BuildResult> RestoreAsync(
        string projectPath,
        CancellationToken cancellationToken = default) {
        var args = $"restore \"{projectPath}\" --nologo";
        return await ExecuteAsync(args, projectPath, null, cancellationToken);
    }

    /// <summary>
    /// Executes dotnet CLI with the specified arguments.
    /// </summary>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="outputPath">Optional output path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The build result.</returns>
    private async Task<BuildResult> ExecuteAsync(
        string arguments,
        string projectPath,
        string? outputPath,
        CancellationToken cancellationToken) {
        var dotnetPath = _toolLocator.LocateDotnetCli();

        var startTime = DateTime.UtcNow;
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var processStartInfo = new ProcessStartInfo {
            FileName = dotnetPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory()
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
            return BuildResult.Successful(output, duration, outputPath);
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
    /// Parses error messages from dotnet CLI output.
    /// </summary>
    private static List<string> ParseErrors(string output) {
        var errors = new List<string>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach(var line in lines) {
            if(ErrorPattern.IsMatch(line)) {
                errors.Add(line.Trim());
            }
        }

        return errors;
    }

    /// <summary>
    /// Parses warning messages from dotnet CLI output.
    /// </summary>
    private static List<string> ParseWarnings(string output) {
        var warnings = new List<string>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach(var line in lines) {
            if(WarningPattern.IsMatch(line)) {
                warnings.Add(line.Trim());
            }
        }

        return warnings;
    }
}
