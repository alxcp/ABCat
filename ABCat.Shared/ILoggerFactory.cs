using Component.Infrastructure;

namespace ABCat.Shared
{
    public interface ILoggerFactory : IExtComponent
    {
        /// <summary>
        ///     Gets the logger for the specified name, creating it if necessary.
        /// </summary>
        /// <param name="name">The name to create the logger for.</param>
        /// <returns>The newly-created logger.</returns>
        ILog GetLogger(string name);

        void Init();

        /// <summary>
        ///     Creates a logger for the specified name.
        /// </summary>
        /// <param name="name">The name to create the logger for</param>
        /// <returns>The newly-created logger.</returns>
        ILog CreateLogger(string name);
    }
}