using System.Diagnostics;
using System.IO;
using ABCat.Shared.Plugins.Downloaders;
using JetBrains.Annotations;

namespace ABCat.Plugins.TorrentFileDownloaders
{
    public abstract class ExternalAppTorrentFileDownloaderBase : ITorrentFileDownloaderPlugin
    {
        public abstract string DisplayName { get; }

        public virtual void Download(string target, string destinationFolder)
        {
            var commandLineArguments = GetCommandLineArguments(target, destinationFolder);
            var appPath = GetExternalAppPath();
            if (!appPath.IsNullOrEmpty())
                Process.Start(appPath, commandLineArguments);
        }

        public bool IsExists => File.Exists(GetExternalAppPath());

        public void Dispose()
        {
        }

        public void FixComponentConfig()
        {
        }

        protected abstract string GetCommandLineArguments(string target, string destinationFolder);

        [CanBeNull]
        protected abstract string GetExternalAppPath();
    }
}