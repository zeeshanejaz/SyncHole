using System;
using System.Collections.Generic;

namespace SyncHole.Core.Manifest
{
    public class ManifestItem
    {
        public string FilePath { get; set; }
        public long CurrentPosition { get; set; }
        public string ContainerName { get; set; }
        public string ArchiveId { get; set; }
        public string UploadId { get; set; }
        public List<string> ChunkHashes { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string Status { get; set; }
        public long ChunkSize { get; set; }
    }
}