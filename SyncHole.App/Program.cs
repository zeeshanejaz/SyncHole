using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncHole.App.Console;
using SyncHole.App.Service;
using SyncHole.App.Service.Worker;
using SyncHole.App.Utility;
using SyncHole.Core.Client;
using SyncHole.Core.Client.AWS;
using SyncHole.Core.Manifest;
using System.IO;
using System.Threading.Tasks;

namespace SyncHole.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await StartHostAsync();
        }

        private static async Task StartHostAsync()
        {
            var hostBuilder = new HostBuilder()
             .UseContentRoot(Directory.GetCurrentDirectory())
#if DEBUG
             .UseEnvironment("Development")
#endif
             .ConfigureAppConfiguration((hostingContext, config) =>
             {
                 var env = hostingContext.HostingEnvironment;
                 config.SetBasePath(env.ContentRootPath)
                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{env.EnvironmentName}.json",
                         optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();
             })
             .ConfigureLogging((hostContext, configLogging) => { configLogging.ClearProviders(); })
             .ConfigureServices((hostContext, services) =>
             {
                 //hide console messages
                 services.Configure<ConsoleLifetimeOptions>(
                     options => options.SuppressStatusMessages = true);

                 //setup the configraution manager
                 var configManager = new ConfigManager(hostContext.Configuration);
                 services.AddSingleton<IConfigManager>(configManager);

                 //use json format for reading / writing the manifest
                 var builder = new JsonManifestBuilder();
                 var manifest = new SyncManifest(configManager, builder);

                 //load manifest from file
                 manifest.ReloadAsync().Wait();
                 services.AddSingleton(manifest);

                 //fetch the path to monitor
                 var path = configManager.SyncDirectory;
                 if (!Directory.Exists(path))
                 {
                     Directory.CreateDirectory(path);
                 }

                 //show welcome info
                 ConsolePrinter.PrintWelcome(path);

                 //read config for aws credentials
                 var cred = new AWSCredentials();
                 hostContext.Configuration.Bind("AWSCredentials", cred);

                 //create and register aws glacier client
                 var client = new AWSGlacierClient(cred);
                 services.AddSingleton<IStorageClient>(client);

                 //add the sync daemon
                 services.AddHostedService<SyncHoleService>();

                 //register worker factory
                 services.AddSingleton<IWorkerFactory, WorkerFactory>();
             });

            await hostBuilder.RunConsoleAsync();
        }
    }
}
