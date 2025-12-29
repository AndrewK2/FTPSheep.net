using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace FTPSheep.VisualStudio.Services;

/// <summary>
/// Service for managing per-project profile associations.
/// </summary>
public class ProjectAssociationService
{
    private readonly VsSolutionService solutionService;
    private Dictionary<string, string> projectProfileMap = new();
    private string? cachedAssociationsFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectAssociationService"/> class.
    /// </summary>
    public ProjectAssociationService(VsSolutionService solutionService)
    {
        this.solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
    }

    /// <summary>
    /// Associates a profile with a project.
    /// </summary>
    public async Task AssociateProfileAsync(string projectPath, string profilePath)
    {
        await LoadAssociationsAsync();

        var projectKey = GetProjectKey(projectPath);
        projectProfileMap[projectKey] = profilePath;

        await SaveAssociationsAsync();
    }

    /// <summary>
    /// Gets the associated profile for a project.
    /// </summary>
    public async Task<string?> GetAssociatedProfileAsync(string projectPath)
    {
        await LoadAssociationsAsync();

        var projectKey = GetProjectKey(projectPath);
        return projectProfileMap.TryGetValue(projectKey, out var profile) ? profile : null;
    }

    /// <summary>
    /// Removes the profile association for a project.
    /// </summary>
    public async Task RemoveAssociationAsync(string projectPath)
    {
        await LoadAssociationsAsync();

        var projectKey = GetProjectKey(projectPath);
        if (projectProfileMap.Remove(projectKey))
        {
            await SaveAssociationsAsync();
        }
    }

    /// <summary>
    /// Gets all project-profile associations.
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllAssociationsAsync()
    {
        await LoadAssociationsAsync();
        return new Dictionary<string, string>(projectProfileMap);
    }

    /// <summary>
    /// Gets the last-used profile for a project (from associations or history).
    /// </summary>
    public async Task<string?> GetLastUsedProfileAsync(string projectPath)
    {
        // Check association first
        var associated = await GetAssociatedProfileAsync(projectPath);
        if (associated != null)
            return associated;

        // TODO: Fall back to deployment history when implemented
        return null;
    }

    /// <summary>
    /// Gets a project key for storage (relative path from solution root).
    /// </summary>
    private string GetProjectKey(string projectPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var solutionDir = solutionService.GetSolutionDirectory();
        if (string.IsNullOrEmpty(solutionDir))
            return projectPath; // No solution loaded, use absolute path

        // Use relative path from solution root for portability
        return Path.GetRelativePath(solutionDir, projectPath);
    }

    /// <summary>
    /// Loads associations from the solution's .vs folder.
    /// </summary>
    private async Task LoadAssociationsAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var filePath = GetAssociationsFilePath();
        if (filePath == null || !File.Exists(filePath))
        {
            projectProfileMap = new Dictionary<string, string>();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            projectProfileMap = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            // If deserialization fails, start with empty dictionary
            projectProfileMap = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Saves associations to the solution's .vs folder.
    /// </summary>
    private async Task SaveAssociationsAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var filePath = GetAssociationsFilePath();
        if (filePath == null)
            return; // No solution loaded

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(projectProfileMap, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FTPSheep: Failed to save project associations: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the file path for storing project associations.
    /// </summary>
    private string? GetAssociationsFilePath()
    {
        if (cachedAssociationsFilePath != null)
            return cachedAssociationsFilePath;

        ThreadHelper.ThrowIfNotOnUIThread();

        var solutionDir = solutionService.GetSolutionDirectory();
        if (string.IsNullOrEmpty(solutionDir))
            return null; // No solution loaded

        var vsFolder = Path.Combine(solutionDir, ".vs", "FTPSheep");
        cachedAssociationsFilePath = Path.Combine(vsFolder, "project-associations.json");
        return cachedAssociationsFilePath;
    }
}
