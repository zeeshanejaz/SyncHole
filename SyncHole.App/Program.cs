using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncHole.Core.Client;
using SyncHole.Core.Client.AWS;
using System.IO;
using System.Threading.Tasks;

namespace SyncHole.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
             .UseContentRoot(Directory.GetCurrentDirectory())
             .UseEnvironment("Development")
             .ConfigureAppConfiguration((hostingContext, config) =>
             {
                 var env = hostingContext.HostingEnvironment;
                 config.SetBasePath(env.ContentRootPath)
                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{env.EnvironmentName}.json",
                         optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();
             })
             .ConfigureLogging((hostContext, configLogging) => { configLogging.AddDebug(); })
             .ConfigureServices((hostContext, services) =>
             {
                 //setup the configraution manager
                 var configManager = new ConfigManager(hostContext.Configuration);
                 services.AddSingleton<IConfigManager>(configManager);

                 //fetch the path to monitor
                 var path = configManager.SyncDirectory;
                 if (!Directory.Exists(path))
                 {
                     Directory.CreateDirectory(path);
                 }

                 //create the file system watcher
                 var fsWatcher = new FileSystemWatcher(path);

                 //show welcome info
                 ConsolePrinter.PrintWelcome(path);

                 //setup the file system watcher
                 services.AddSingleton(fsWatcher);

                 //read config for aws credentials
                 var cred = new AWSCredentials();
                 hostContext.Configuration.Bind("AWSCredentials", cred);

                 //create and register aws glacier client
                 var client = new AWSGlacierClient(cred);
                 services.AddSingleton<IStorageClient>(client);

                 //add the sync deamon
                 services.AddHostedService<SyncHoleService>();

                 //register worker factory
                 services.AddSingleton<IWorkerFactory, WorkerFactory>();
             });

            await hostBuilder.RunConsoleAsync();
        }
    }
}
