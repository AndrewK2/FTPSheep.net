using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using FTPSheep.Core.Interfaces;
using FTPSheep.Core.Services;
using FTPSheep.BuildTools.Services;
using FTPSheep.Protocols.Services;
using FTPSheep.VisualStudio.Modern.Services;

namespace FTPSheep.VisualStudio.Modern;

/// <summary>
/// Extension entry point for FTPSheep Visual Studio Extension.
/// This extension provides GUI-based deployment functionality for FTPSheep,
/// running out-of-process for improved performance and reliability.
/// </summary>
[VisualStudioContribution]
internal class FTPSheepExtension : Extension
{
    /// <summary>
    /// Extension configuration including metadata for Visual Studio Marketplace.
    /// </summary>
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "FTPSheep.VisualStudio.b4e8c2a1-9d5f-4e1a-8b3c-7f2a6d4e1c9b",
            version: this.ExtensionAssemblyVersion,
            publisherName: "FTPSheep",
            displayName: "FTPSheep - FTP Deployment Tool",
            description: "Deploy ASP.NET applications to FTP servers with concurrent uploads, progress tracking, and profile management.")
    };

    /// <summary>
    /// Configure services for dependency injection.
    /// This is called once when the extension is loaded.
    /// </summary>
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);

        // FTPSheep Core Services - reuse existing business logic
        ConfigureFTPSheepServices(serviceCollection);

        // Visual Studio integration services
        ConfigureVSServices(serviceCollection);
    }

    private void ConfigureFTPSheepServices(IServiceCollection services)
    {
        // Core services from FTPSheep.Core (all .NET 8 - no compatibility issues!)

        // Credential and Profile management
        services.AddSingleton<ICredentialStore, CredentialStore>();
        services.AddSingleton<IProfileRepository, FileSystemProfileRepository>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<ProfileService>(); // Also register concrete type for direct access

        // Deployment services
        services.AddSingleton<DeploymentCoordinator>();
        services.AddSingleton<IDeploymentHistoryService, JsonDeploymentHistoryService>();
        services.AddSingleton<JsonDeploymentHistoryService>(); // Also register concrete type
        services.AddSingleton<AppOfflineManager>();
        services.AddSingleton<ExclusionPatternMatcher>();
        services.AddSingleton<FileComparisonService>();
        services.AddSingleton<DpapiEncryptionService>();
        services.AddSingleton<PublishProfileParser>();
        services.AddSingleton<PublishProfileConverter>();

        // BuildTools services
        services.AddSingleton<BuildService>();
        services.AddSingleton<ProjectFileParser>();
        services.AddSingleton<ProjectTypeClassifier>();
        services.AddSingleton<BuildToolLocator>();
        services.AddSingleton<MsBuildWrapper>();
        services.AddSingleton<MsBuildExecutor>();
        services.AddSingleton<DotnetCliExecutor>();
        services.AddSingleton<PublishOutputScanner>();

        // Protocol services
        services.AddSingleton<FtpClientFactory>();
        services.AddSingleton<FtpClientService>();
    }

    private void ConfigureVSServices(IServiceCollection services)
    {
        // VS-specific services for integrating with Visual Studio

        // Register VS integration services
        services.AddSingleton<VsOutputWindowService>();
        services.AddSingleton<VsStatusBarService>();
        services.AddSingleton<VsDeploymentOrchestrator>();

        // These services require VisualStudioExtensibility which is provided
        // by the extension framework through constructor injection
    }
}
