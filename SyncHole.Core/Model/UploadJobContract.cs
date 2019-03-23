using System.Collections.Generic;

namespace SyncHole.Core.Model
{
    public class UploadJob
    {
        public string UploadId { get; set; }

        public string VaultName { get; set; }

        public long ChunkSize { get; set; }

        public List<string> ChunkChecksums { get; set; } = new List<string>();

        public long CurrentPosition { get; set; } = 0;

        public long TotalSize { get; set; }

        public string FilePath { get; set; }
    }
}