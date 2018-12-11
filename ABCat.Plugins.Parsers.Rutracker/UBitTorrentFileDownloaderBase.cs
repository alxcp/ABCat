namespace ABCat.Plugins.Parsers.Rutracker
{
    public abstract class UBitTorrentFileDownloaderBase : ExternalAppTorrentFileDownloaderBase
    {
        protected override string GetCommandLineArguments(string target, string destinationFolder)
        {
            return $"/directory \"{destinationFolder}\" \"{target}\"";
        }
    }
}