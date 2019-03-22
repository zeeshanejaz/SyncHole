namespace SyncHole.App
{
    public interface IConfigManager
    {
        string SyncDirectory { get; }
        int SyncBatchSize { get; }
        string ArchiveNameFormat { get; }
    }
}
