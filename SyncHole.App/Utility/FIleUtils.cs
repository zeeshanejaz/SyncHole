using System;
using System.IO;
using System.Threading;

namespace SyncHole.App.Utility
{
    public static class FileUtils
    {
        public static void WaitUntilLocked(string fullPath)
        {
            //timeout in 15 minutes
            var expiry = DateTime.UtcNow.AddMinutes(15);
            var fileInfo = new FileInfo(fullPath);

            while (DateTime.UtcNow < expiry && fileInfo.Exists)
            {
                try
                {
                    using (var fs = File.Open(fullPath, FileMode.Open))
                    {
                        //lock removed
                        fs.Close();
                        break;
                    }
                }
                catch (IOException)
                {
                    //operation hasn't finished, let's wait a moment
                    Thread.Sleep(1000);
                }
            }

            if (DateTime.UtcNow >= expiry)
            {
                throw new FileLoadException("File lock wait timeout");
            }

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(fullPath);
            }
        }
    }
}
