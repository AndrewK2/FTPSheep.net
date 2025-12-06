using FTPSheep.CLI.Commands;
using NLog;
using NLog.Extensions.Logging;
using Spectre.Console.Cli;

namespace FTPSheep.CLI;

internal class Program
{
    private static int Main(string[] args)
    {
        var logger = LogManager.GetCurrentClassLogger();

        try
        {
            logger.Info("FTPSheep.NET starting up...");

            var app = new CommandApp();

            app.Configure(config =>
            {
                config.SetApplicationName("ftpsheep");
                config.SetApplicationVersion("0.1.0");

                config.AddCommand<DeployCommand>("deploy")
                    .WithDescription("Deploy a .NET application to FTP server")
                    .WithExample(new[] { "deploy", "--profile", "production" })
                    .WithExample(new[] { "deploy", "--yes" });

                config.AddBranch("profile", profile =>
                {
                    profile.SetDescription("Manage deployment profiles");

                    profile.AddCommand<ProfileListCommand>("list")
                        .WithDescription("List all deployment profiles");

                    profile.AddCommand<ProfileCreateCommand>("create")
                        .WithDescription("Create a new deployment profile");
                });

                config.AddCommand<ImportCommand>("import")
                    .WithDescription("Import Visual Studio publish profile")
                    .WithExample(new[] { "import", "Properties/PublishProfiles/FTP.pubxml" });

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
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Stopped program because of exception");
            return 1;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}
