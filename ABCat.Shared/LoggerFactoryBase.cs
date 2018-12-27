using System.Collections.Generic;
using Component.Infrastructure;

namespace ABCat.Shared
{
    public abstract class LoggerFactoryBase : ILoggerFactory
    {
        /// <summary>
        ///     Maps types to their loggers.
        /// </summary>
        private readonly Dictionary<string, ILog> _loggers = new Dictionary<string, ILog>();

        public Config Config { get; set; }

        /// <summary>
        ///     Gets the logger for the specified name, creating it if necessary.
        /// </summary>
        /// <param name="name">The name to create the logger for.</param>
        /// <returns>The newly-created logger.</returns>
        public ILog GetLogger(string name)
        {
            lock (_loggers)
            {
                if (_loggers.ContainsKey(name))
                {
                    return _loggers[name];
                }

                var logger = CreateLogger(name);
                _loggers.Add(name, logger);

                return logger;
            }
        }

        public abstract void Init();

        /// <summary>
        ///     Creates a logger for the specified name.
        /// </summary>
        /// <param name="name">The name to create the logger for</param>
        /// <returns>The newly-created logger.</returns>
        public abstract ILog CreateLogger(string name);

        public virtual void Dispose()
        {
        }

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }
    }
}