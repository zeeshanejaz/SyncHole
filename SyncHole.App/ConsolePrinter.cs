using System;
using System.Diagnostics;
using System.Reflection;

namespace SyncHole.App
{
    public static class ConsolePrinter
    {
        public static void PrintWelcome(string syncPath)
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
