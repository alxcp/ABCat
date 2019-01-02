using System.IO;
using System.Linq;
using ABCat.Shared;
using Component.Infrastructure.Factory;

namespace ABCat.Plugins.TorrentFileDownloaders
{
    [PerCallComponentInfo("2.2")]
    public class UTorrentFileDownloader : UBitTorrentFileDownloaderBase
    {
        public override string DisplayName => "µtorrent";

        protected override string GetExternalAppPath()
        {
            var ia = Extensions.GetInstalledApplications()
                .FirstOrDefault(item => item.DisplayName.ToLower().Contains(DisplayName.ToLower()));
            if (ia != null)
            {
                return Path.Combine(ia.InstallLocation, "utorrent.exe");
            }

            return null;
        }
    }
}