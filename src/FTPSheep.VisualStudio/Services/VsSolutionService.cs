using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FTPSheep.VisualStudio.Services;

/// <summary>
/// Service for querying Visual Studio solution and project information.
/// </summary>
public class VsSolutionService
{
    private readonly IVsSolution solution;

    /// <summary>
    /// Initializes a new instance of the <see cref="VsSolutionService"/> class.
    /// </summary>
    public VsSolutionService(IVsSolution solution)
    {
        this.solution = solution ?? throw new ArgumentNullException(nameof(solution));
    }

    /// <summary>
    /// Gets the full path to the current solution.
    /// </summary>
    public string GetSolutionPath()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        solution.GetSolutionInfo(out string solutionDir, out _, out _);
        return solutionDir ?? string.Empty;
    }

    /// <summary>
    /// Gets the full path to the current solution asynchronously.
    /// </summary>
    public async Task<string> GetSolutionPathAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        return GetSolutionPath();
    }

    /// <summary>
    /// Gets the directory containing the solution file.
    /// </summary>
    public string GetSolutionDirectory()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var solutionPath = GetSolutionPath();
        return !string.IsNullOrEmpty(solutionPath) ? Path.GetDirectoryName(solutionPath) ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Gets the directory containing the solution file asynchronously.
    /// </summary>
    public async Task<string> GetSolutionDirectoryAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        return GetSolutionDirectory();
    }

    // TODO: Implement these methods using IVsSolution instead of Community.VisualStudio.Toolkit
    // The Community.VisualStudio.Toolkit package doesn't properly support .NET 8

    ///// <summary>
    ///// Gets the currently active project.
    ///// </summary>
    //public async Task<Project?> GetActiveProjectAsync()
    //{
    //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    //    return await VS.Solutions.GetActiveProjectAsync();
    //}

    ///// <summary>
    ///// Gets the currently selected item in Solution Explorer.
    ///// </summary>
    //public async Task<SolutionItem?> GetSelectedItemAsync()
    //{
    //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    //    return await VS.Solutions.GetActiveItemAsync();
    //}

    ///// <summary>
    ///// Determines if a project is a web project (ASP.NET).
    ///// </summary>
    //public bool IsWebProject(Project? project)
    //{
    //    if (project == null) return false;

    //    ThreadHelper.ThrowIfNotOnUIThread();

    //    // Check for ASP.NET project type GUIDs
    //    var projectTypeGuids = project.GetProjectTypeGuids();

    //    // Web Application Project
    //    if (projectTypeGuids.Contains("{349c5851-65df-11da-9384-00065b846f21}"))
    //        return true;

    //    // Web Site Project
    //    if (projectTypeGuids.Contains("{E24C65DC-7377-472b-9ABA-BC803B73C61A}"))
    //        return true;

    //    return false;
    //}

    ///// <summary>
    ///// Determines if a project is a web project asynchronously.
    ///// </summary>
    //public async Task<bool> IsWebProjectAsync(Project? project)
    //{
    //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    //    return IsWebProject(project);
    //}
}
