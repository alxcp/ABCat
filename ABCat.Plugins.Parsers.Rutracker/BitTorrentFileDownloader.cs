﻿using System.IO;
using System.Linq;
using ABCat.Shared;
using Component.Infrastructure.Factory;

namespace ABCat.Plugins.Parsers.Rutracker
{
    [PerCallComponentInfo("2.2")]
    public class BitTorrentFileDownloader : UBitTorrentFileDownloaderBase
    {
        protected override string GetExternalAppPath()
        {
            var ia = Extensions.GetInstalledApplications().FirstOrDefault(item => item.DisplayName.ToLower().Contains(DisplayName.ToLower()));
            if (ia != null)
            {
                return Path.Combine(ia.InstallLocation, "bittorrent.exe");
            }

            return null;
        }

        public override string DisplayName => "BitTorrent";
    }
}