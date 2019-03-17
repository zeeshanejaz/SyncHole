using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncHole.Core.Client;
using SyncHole.Core.Client.AWS;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
                 //fetch the path to monitor
                 var path = hostContext.Configuration.GetValue<string>("SyncOptions:SyncDirectory");
                 if (!Directory.Exists(path))
                 {
                     Directory.CreateDirectory(path);
                 }
                 //create the file system watcher
                 var fsWatcher = new FileSystemWatcher(path);

                 //show welcome info
                 PrintWelcome(path);

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

        private static void PrintWelcome(string syncPath)
        {
            var version = GetVersion();
            var preColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(@"..................................................................................");
            Console.WriteLine(@"............................................$$\...$$\...........$$\...............");
            Console.WriteLine(@"............................................$$.|..$$.|..........$$.|..............");
            Console.WriteLine(@".....$$$$$$$\.$$\...$$\.$$$$$$$\...$$$$$$$\.$$.|..$$.|.$$$$$$\..$$.|.$$$$$$\......");
            Console.WriteLine(@"....$$.._____|$$.|..$$.|$$..__$$\.$$.._____|$$$$$$$$.|$$..__$$\.$$.|$$..__$$\.....");
            Console.WriteLine(@"....\$$$$$$\..$$.|..$$.|$$.|..$$.|$$./......$$..__$$.|$$./..$$.|$$.|$$$$$$$$.|....");
            Console.WriteLine(@".....\____$$\.$$.|..$$.|$$.|..$$.|$$.|......$$.|..$$.|$$.|..$$.|$$.|$$...____|....");
            Console.WriteLine(@"....$$$$$$$..|\$$$$$$$.|$$.|..$$.|\$$$$$$$\.$$.|..$$.|\$$$$$$..|$$.|\$$$$$$$\.....");
            Console.WriteLine(@"....\_______/..\____$$.|\__|..\__|.\_______|\__|..\__|.\______/.\__|.\_______|....");
            Console.WriteLine(@"..............$$\...$$.|..........................................................");
            Console.WriteLine(@"..............\$$$$$$..|..........................................................");
            Console.WriteLine(@"...............\______/...........................................................");
            Console.WriteLine($"v{version}".PadLeft(82, '.'));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Sync Path: {syncPath}");

            Console.ForegroundColor = preColor;
        }

        private static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;
            return version;
        }
    }
}
