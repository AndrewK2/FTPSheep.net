using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FTPSheep.BuildTools.Services;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Services;
using FTPSheep.Protocols.Services;
using FTPSheep.VisualStudio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace FTPSheep.VisualStudio;

/// <summary>
/// This is the class that implements the package exposed by this assembly.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuids.FTPSheepPackageString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
//[ProvideToolWindow(typeof(ToolWindows.FTPSheepToolWindow))]
public sealed class FTPSheepPackage : AsyncPackage
{
    private IServiceProvider? serviceProvider;

    /// <summary>
    /// Initializes the package asynchronously.
    /// </summary>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        // When initialized asynchronously, the current thread may be a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        try
        {
            // Configure dependency injection
            serviceProvider = ConfigureServices();

            // Initialize commands (when implemented)
            // await Commands.DeployProjectCommand.InitializeAsync(this, serviceProvider);
            // await Commands.ManageProfilesCommand.InitializeAsync(this, serviceProvider);

            // Log successful initialization
            var outputService = serviceProvider?.GetService<VsOutputWindowService>();
            if (outputService != null)
            {
                await outputService.WriteLineAsync("FTPSheep extension initialized successfully");
            }
        }
        catch (Exception ex)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VsShellUtilities.ShowMessageBox(
                this,
                $"Failed to initialize FTPSheep extension: {ex.Message}",
                "FTPSheep Extension Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    /// <summary>
    /// Configures the dependency injection container with all required services.
    /// </summary>
    private IServiceProvider ConfigureServices()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var services = new ServiceCollection();

        // Register VS Services
        var vsOutputWindow = (IVsOutputWindow?)GetService(typeof(SVsOutputWindow));
        var vsStatusbar = (IVsStatusbar?)GetService(typeof(SVsStatusbar));
        var vsSolution = (IVsSolution?)GetService(typeof(SVsSolution));

        if (vsOutputWindow != null)
            services.AddSingleton(vsOutputWindow);
        if (vsStatusbar != null)
            services.AddSingleton(vsStatusbar);
        if (vsSolution != null)
            services.AddSingleton(vsSolution);

        // Register FTPSheep Core Services (reuse existing!)
        services.AddSingleton<ICredentialStore, CredentialStore>();
        services.AddSingleton<IProfileRepository, FileSystemProfileRepository>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<BuildService>();
        services.AddSingleton<FtpClientFactory>();
        services.AddSingleton<DeploymentCoordinator>();
        services.AddSingleton<IDeploymentHistoryService, JsonDeploymentHistoryService>();

        // Register VS-specific Services
        services.AddSingleton<VsOutputWindowService>();
        services.AddSingleton<VsStatusBarService>();
        services.AddSingleton<VsErrorListService>();
        services.AddSingleton<VsSolutionService>();
        services.AddSingleton<ProjectAssociationService>();
        services.AddSingleton<VsDeploymentOrchestrator>();

        // TODO: Register Logging when needed
        // services.AddLogging(...);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider ServiceProvider => serviceProvider ?? throw new InvalidOperationException("Service provider not initialized");
}

/// <summary>
/// Package GUIDs for the FTPSheep extension.
/// </summary>
public static class PackageGuids
{
    public const string FTPSheepPackageString = "8f9c3e4a-1b2d-4c5e-9a7b-3f8e6d2c1a4b";
    public static readonly Guid FTPSheepPackage = new(FTPSheepPackageString);
}
