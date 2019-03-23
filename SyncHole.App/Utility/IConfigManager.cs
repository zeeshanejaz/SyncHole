namespace SyncHole.App.Utility
{
    public interface IConfigManager
    {
        string SyncDirectory { get; }
        string VaultNameFormat { get; }
        string SyncFileFilter { get; }
        string IgnoreFileRegex { get; }
    }
}
