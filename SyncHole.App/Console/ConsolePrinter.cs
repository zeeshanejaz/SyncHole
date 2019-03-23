using System;
using System.Diagnostics;
using System.Reflection;

namespace SyncHole.App.Console
{
    public static class ConsolePrinter
    {
        public static void PrintWelcome(string syncPath)
        {
            var version = GetVersion();
            var preColor = System.Console.ForegroundColor;

            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            System.Console.WriteLine(@"..................................................................................");
            System.Console.WriteLine(@"............................................$$\...$$\...........$$\...............");
            System.Console.WriteLine(@"............................................$$.|..$$.|..........$$.|..............");
            System.Console.WriteLine(@".....$$$$$$$\.$$\...$$\.$$$$$$$\...$$$$$$$\.$$.|..$$.|.$$$$$$\..$$.|.$$$$$$\......");
            System.Console.WriteLine(@"....$$.._____|$$.|..$$.|$$..__$$\.$$.._____|$$$$$$$$.|$$..__$$\.$$.|$$..__$$\.....");
            System.Console.WriteLine(@"....\$$$$$$\..$$.|..$$.|$$.|..$$.|$$./......$$..__$$.|$$./..$$.|$$.|$$$$$$$$.|....");
            System.Console.WriteLine(@".....\____$$\.$$.|..$$.|$$.|..$$.|$$.|......$$.|..$$.|$$.|..$$.|$$.|$$...____|....");
            System.Console.WriteLine(@"....$$$$$$$..|\$$$$$$$.|$$.|..$$.|\$$$$$$$\.$$.|..$$.|\$$$$$$..|$$.|\$$$$$$$\.....");
            System.Console.WriteLine(@"....\_______/..\____$$.|\__|..\__|.\_______|\__|..\__|.\______/.\__|.\_______|....");
            System.Console.WriteLine(@"..............$$\...$$.|..........................................................");
            System.Console.WriteLine(@"..............\$$$$$$..|..........................................................");
            System.Console.WriteLine(@"...............\______/...........................................................");
            System.Console.WriteLine($"v{version}".PadLeft(82, '.'));

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"Sync Path: {syncPath}");

            System.Console.ForegroundColor = preColor;
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
