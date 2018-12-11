using System.Diagnostics;
using System.IO;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.Plugins.Parsers.Rutracker
{
    public abstract class ExternalAppTorrentFileDownloaderBase : ITorrentFileDownloaderPlugin
    {
        protected abstract string GetCommandLineArguments(string target, string destinationFolder);
        [CanBeNull]
        protected abstract string GetExternalAppPath();

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

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }
    }
}