namespace SyncHole.Core.Manifest
{
    public class ManifestItem
    {
        [Position(0)]
        public string FilePath { get; set; }

        [Position(1)]
        public string CurrentPosition { get; set; }

        [Position(2)]
        public string ContainerName { get; set; }

        [Position(3)]
        public string ArchiveId { get; set; }

        [Position(4)]
        public string UploadId { get; set; }

        [Position(5)]
        public string ProcessedAt { get; set; }

        [Position(6)]
        public string Status { get; set; }
    }
}