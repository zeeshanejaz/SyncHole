using SyncHole.Core.Model;
using System;
using System.IO;

namespace SyncHole.App.Console
{
    public class ProgressPrinter
    {
        private readonly int _lineNumber;

        public ProgressPrinter()
        {
            _lineNumber = System.Console.CursorTop;
        }

        public void PrintProgress(UploadJob job)
        {
            var percent = (int)Math.Floor((double)job.CurrentPosition / job.TotalSize * 100);
            var fileName = Path.GetFileName(job.FilePath);

            PrintProgress(fileName, percent);

            if (percent == 100)
            {
                PrintDone();
            }
        }

        private void PrintDone()
        {
            var preColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.CursorTop = _lineNumber;
            System.Console.CursorLeft = 57;
            System.Console.WriteLine(" Done");
            System.Console.Write("Waiting for next job...".PadRight(57));
            System.Console.ForegroundColor = preColor;
            System.Console.CursorLeft = 0;
        }

        private void PrintProgress(string fileName, int percent)
        {
            const int barWidth = 50;
            var progressBar = (int)Math.Floor((double)percent / 100 * barWidth);

            //set position
            System.Console.CursorTop = _lineNumber;
            System.Console.CursorLeft = 0;

            //the title message
            System.Console.Write("Currently uploading: ");
            if (fileName.Length > 36)
            {
                fileName = fileName.Substring(0, 30)
                           + ".." + Path.GetExtension(fileName);
            }
            var preColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(fileName);
            System.Console.ForegroundColor = preColor;

            //translate to percentage
            System.Console.Write(percent.ToString().PadLeft(3) + "% [");
            System.Console.ForegroundColor = ConsoleColor.DarkCyan;
            System.Console.Write(string.Empty.PadRight(progressBar, '█').PadRight(barWidth, '░'));
            System.Console.ForegroundColor = preColor;

            System.Console.Write("]");
        }

    }
}
