using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Downloaders;
using ABCat.Shared.Plugins.Sites;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.Parsers.Rutracker
{
    [UsedImplicitly]
    [SingletoneComponentInfo("2.2")]
    public class RutrackerTorrentFilesDownloader : RecordTargetDownloaderBase
    {
        private RutrackerTorrentFilesDownloaderConfig _config;

        public override void DownloadRecordTarget(string loginCoockies, IAudioBook record, IDbContainer dbContainer, CancellationToken cancellationToken)
        {
            var config = Config.Load<RutrackerTorrentFilesDownloaderConfig>();

            Directory.CreateDirectory(config.TorrentFilesFolder);

            var userData = dbContainer.UserDataSet.CreateUserData();
            userData.RecordGroupKey = record.GroupKey;
            userData.RecordKey = record.Key;

            var targetLibraryPath = GetAbsoluteLibraryPath(record);
            userData.LocalPath = targetLibraryPath;

            var downloader = Context.I.ComponentFactory.GetCreators<ITorrentFileDownloaderPlugin>()
                .Select(item => item.GetInstance<ITorrentFileDownloaderPlugin>())
                .FirstOrDefault(item => item.DisplayName == config.TorrentClientName);

            if (downloader != null && downloader.IsExists)
                downloader.Download(record.MagnetLink, targetLibraryPath);

            dbContainer.UserDataSet.AddUserData(userData);
            dbContainer.SaveChanges();
        }

        public override string GetAbsoluteLibraryPath(IAudioBook record)
        {
            var config = Config.Load<RutrackerTorrentFilesDownloaderConfig>();
            var bookPath = GetRecordTargetLibraryPath(record);
            return Path.Combine(config.AudioCatalogFolder, bookPath);
        }

        public override string GetRecordTargetLibraryPath(IAudioBook record)
        {
            var result = GetFileName(record.Author ?? record.Title);
            return result;
        }

        private string GetFileName(string rawFileName)
        {
            var sb = new StringBuilder();

            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            foreach (var c in rawFileName)
            {
                if (invalidChars.Contains(c)) sb.Append("_");
                else sb.Append(c);
            }

            return sb.ToString();
        }

        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            _config = Config.Load<RutrackerTorrentFilesDownloaderConfig>();
            var checkResult = _config.Check(correct);
            incorrectConfig = checkResult ? null : _config;
            return checkResult;
        }
    }
}