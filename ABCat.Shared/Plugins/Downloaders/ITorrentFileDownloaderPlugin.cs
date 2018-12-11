using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Downloaders
{
    /// <summary>
    /// An interface for torrent files downloader implementations
    /// </summary>
    public interface ITorrentFileDownloaderPlugin : IExtComponent
    {
        /// <summary>
        /// Display Name for show on UI
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Download torrent to destination directory
        /// </summary>
        /// <param name="target">Magnet URI or Torrent File path</param>
        /// <param name="destinationFolder">Folder to store torrent content</param>
        void Download(string target, string destinationFolder);

        bool IsExists { get; }
    }
}