using FTPSheep.CLI;
using FTPSheep.CLI.Commands;
using FTPSheep.Utilities.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using Spectre.Console.Cli;

var logger = LogManager.GetCurrentClassLogger();

try {
    logger.Info("FTPSheep.NET starting up...");

    var registrations = new ServiceCollection();
    registrations.AddNLog();

    var app = new CommandApp(new TypeRegistrar(registrations));

    app.Configure(config => {
        config.SetApplicationName("ftpsheep");
        config.SetApplicationVersion("0.1.0");

        config.AddCommand<DeployCommand>("deploy")
            .WithDescription("Deploy a .NET application to FTP server")
            .WithExample("deploy", "--file", @"c:\projects\website1\Properties\PublishProfiles\production.ftpsheep")
            .WithExample("deploy", "--profile", "production")
            .WithExample("deploy", "--yes");

        config.AddBranch("profile", profile => {
            profile.SetDescription("Manage deployment profiles");

            profile.AddCommand<ProfileListCommand>("list")
                .WithDescription("List all deployment profiles");

            profile.AddCommand<ProfileCreateCommand>("create")
                .WithDescription("Create a new deployment profile");
        });

        config.AddCommand<ImportCommand>("import")
            .WithDescription("Import Visual Studio publish profile")
            .WithExample("import", "Properties/PublishProfiles/FTP.pubxml");

        config.AddCommand<InitCommand>("init")
            .WithDescription("Initialize a new deployment profile interactively");

#if DEBUG
        config.PropagateExceptions();
        config.ValidateExamples();
#endif
    });

    var result = app.Run(args);

    logger.Info("FTPSheep.NET shutting down with exit code {ExitCode}", result);
    return result;
} catch(Exception ex) {
    logger.Error(ex, "Stopped program because of exception\n" + ex.GetDescription());
    return 1;
} finally {
    LogManager.Shutdown();
}

namespace FTPSheep.CLI {
    file class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar {
        public ITypeResolver Build() {
            return new TypeResolver(builder.BuildServiceProvider());
        }

        public void Register(Type service, Type implementation) {
            builder.AddSingleton(service, implementation);
        }

        public void RegisterInstance(Type service, object implementation) {
            builder.AddSingleton(service, implementation);
        }

        public void RegisterLazy(Type service, Func<object> func) {
            if(func is null) {
                throw new ArgumentNullException(nameof(func));
            }

            builder.AddSingleton(service, (provider) => func());
        }
    }

    file sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable {
        private readonly IServiceProvider provider = provider ?? throw new ArgumentNullException(nameof(provider));

        public object? Resolve(Type? type) {
            return type == null ? null : provider.GetService(type);
        }

        public void Dispose() {
            if(provider is IDisposable disposable) {
                disposable.Dispose();
            }
        }
    }
}