using System.IO;
using System.Linq;
using ABCat.Shared;
using Component.Infrastructure.Factory;

namespace ABCat.Plugins.TorrentFileDownloaders
{
    [PerCallComponentInfo("2.2")]
    public class QBitTorrentFileDownloader : ExternalAppTorrentFileDownloaderBase
    {
        public override string DisplayName => "qBittorrent";

        protected override string GetCommandLineArguments(string target, string destinationFolder)
        {
            return $"\"{target}\" --save-path=\"{destinationFolder}\" --skip-dialog=true";
        }

        protected override string GetExternalAppPath()
        {
            var ia = Extensions.GetInstalledApplications()
                .FirstOrDefault(item => item.DisplayName.ToLower().Contains(DisplayName.ToLower()));
            if (ia != null)
            {
                return Path.Combine(Path.GetDirectoryName(ia.UninstallString.Trim('"')), "qbittorrent.exe");
            }

            return null;
        }
    }
}