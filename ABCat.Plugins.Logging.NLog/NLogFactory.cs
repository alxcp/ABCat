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
            var tar = (FileTarget) LogManager.Configuration.FindTargetByName("run_log");
            tar.DeleteOldFileOnStartup = true;
        }

        public override ILog CreateLogger(string name)
        {
            return new Logger4Net(name);
        }
    }
}