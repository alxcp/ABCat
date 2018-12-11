namespace ABCat.Plugins.TorrentFileDownloaders
{
    public abstract class UBitTorrentFileDownloaderBase : ExternalAppTorrentFileDownloaderBase
    {
        protected override string GetCommandLineArguments(string target, string destinationFolder)
        {
            return $"/directory \"{destinationFolder}\" \"{target}\"";
        }
    }
}