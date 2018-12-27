using System;
using System.IO;
using ABCat.Shared;
using Component.Infrastructure.Factory;
using NLog;
using NLog.Targets;

namespace ABCat.Plugins.Logging.NLog
{
    [PerCallComponentInfo("1.0")]
    public class NLogFactory : LoggerFactoryBase
    {
        public override void Init()
        {
            File.Copy(Path.Combine(Context.I.ComponentFactory.ComponentsFolderPath, "NLog.config"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config"), true);
            LogManager.Configuration = LogManager.Configuration.Reload();
            LogManager.ReconfigExistingLoggers();
            var tar = (FileTarget) LogManager.Configuration.FindTargetByName("run_log");
            LogManager.Configuration.Variables["basedir"] = SharedContext.I.GetAppDataFolderPath("Logs");
            Directory.CreateDirectory(LogManager.Configuration.Variables["basedir"].Text);
            tar.DeleteOldFileOnStartup = false;
        }

        public override ILog CreateLogger(string name)
        {
            return new Logger4Net(name);
        }
    }
}