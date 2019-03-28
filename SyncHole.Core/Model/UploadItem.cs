using System.IO;

namespace SyncHole.Core.Model
{
    public class UploadItem
    {
        public Stream DataStream { get; set; }

        public long ContentLength { get; set; }
    }
}
