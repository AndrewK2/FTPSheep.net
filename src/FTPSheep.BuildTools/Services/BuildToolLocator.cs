using FTPSheep.BuildTools.Exceptions;
using Microsoft.Win32;

namespace FTPSheep.BuildTools.Services;

/// <summary>
/// Locates build tools (MSBuild, dotnet CLI) on the system.
/// </summary>
public class BuildToolLocator {
    private readonly string[] _dotnetSearchPaths = new[]
    {
        @"C:\Program Files\dotnet\dotnet.exe",
        @"C:\Program Files (x86)\dotnet\dotnet.exe"
    };

    private readonly string[] _msbuildSearchPaths = new[]
    {
        // Visual Studio 2022
        @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        // Visual Studio 2019
        @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        // Build Tools
        @"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        @"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    };

    /// <summary>
    /// Locates the dotnet CLI executable on the system.
    /// </summary>
    /// <returns>The full path to dotnet.exe.</returns>
    /// <exception cref="ToolNotFoundException">Thrown when dotnet.exe cannot be found.</exception>
    public string LocateDotnetCli() {
        // Try PATH environment variable first
        var pathDotnet = FindInPath("dotnet.exe");
        if(pathDotnet != null) {
            return pathDotnet;
        }

        // Try well-known locations
        foreach(var searchPath in _dotnetSearchPaths) {
            if(File.Exists(searchPath)) {
                return searchPath;
            }
        }

        throw new ToolNotFoundException("Build tool 'dotnet CLI (dotnet.exe)' was not found on the system.") {
            ToolName = "dotnet CLI (dotnet.exe)"
        };
    }

    /// <summary>
    /// Locates MSBuild executable on the system.
    /// </summary>
    /// <returns>The full path to MSBuild.exe.</returns>
    /// <exception cref="ToolNotFoundException">Thrown when MSBuild.exe cannot be found.</exception>
    public string LocateMSBuild() {
        // Try vswhere first (most reliable for VS installations)
        var vsWherePath = LocateVsWhere();
        if(vsWherePath != null) {
            var msbuildPath = FindMSBuildUsingVsWhere(vsWherePath);
            if(msbuildPath != null) {
                return msbuildPath;
            }
        }

        // Try PATH environment variable
        var pathMsbuild = FindInPath("msbuild.exe");
        if(pathMsbuild != null) {
            return pathMsbuild;
        }

        // Try well-known locations
        foreach(var searchPath in _msbuildSearchPaths) {
            if(File.Exists(searchPath)) {
                return searchPath;
            }
        }

        // Try registry (for older versions)
        var registryPath = FindMSBuildInRegistry();
        if(registryPath != null) {
            return registryPath;
        }

        throw new ToolNotFoundException("Build tool 'MSBuild (MSBuild.exe)' was not found on the system.") {
            ToolName = "MSBuild (MSBuild.exe)"
        };
    }

    /// <summary>
    /// Checks if the dotnet CLI is available on the system.
    /// </summary>
    /// <returns>True if dotnet CLI is available.</returns>
    public bool IsDotnetCliAvailable() {
        try {
            LocateDotnetCli();
            return true;
        } catch(ToolNotFoundException) {
            return false;
        }
    }

    /// <summary>
    /// Checks if MSBuild is available on the system.
    /// </summary>
    /// <returns>True if MSBuild is available.</returns>
    public bool IsMSBuildAvailable() {
        try {
            LocateMSBuild();
            return true;
        } catch(ToolNotFoundException) {
            return false;
        }
    }

    /// <summary>
    /// Gets the version of the dotnet CLI.
    /// </summary>
    /// <returns>The version string of dotnet CLI, or null if not available.</returns>
    public string? GetDotnetCliVersion() {
        try {
            var dotnetPath = LocateDotnetCli();
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = dotnetPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if(process == null) {
                return null;
            }

            var version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? version : null;
        } catch {
            return null;
        }
    }

    private string? FindInPath(string executable) {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if(string.IsNullOrEmpty(pathEnv)) {
            return null;
        }

        var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach(var path in paths) {
            var fullPath = Path.Combine(path, executable);
            if(File.Exists(fullPath)) {
                return fullPath;
            }
        }

        return null;
    }

    private string? LocateVsWhere() {
        var vsWherePath = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        return File.Exists(vsWherePath) ? vsWherePath : null;
    }

    private string? FindMSBuildUsingVsWhere(string vsWherePath) {
        try {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = vsWherePath,
                Arguments = "-latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if(process == null) {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if(process.ExitCode == 0 && !string.IsNullOrEmpty(output)) {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var msbuildPath = lines.FirstOrDefault(line => File.Exists(line));
                return msbuildPath;
            }
        } catch {
            // vswhere failed, fall through to other methods
        }

        return null;
    }

    private string? FindMSBuildInRegistry() {
        try {
            // Try to find MSBuild in registry (for .NET Framework 4.x)
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0");
            var msbuildPath = key?.GetValue("MSBuildToolsPath") as string;

            if(!string.IsNullOrEmpty(msbuildPath)) {
                var fullPath = Path.Combine(msbuildPath, "MSBuild.exe");
                if(File.Exists(fullPath)) {
                    return fullPath;
                }
            }
        } catch {
            // Registry access failed
        }

        return null;
    }
}
