using System;

namespace ABCat.Shared
{
    public interface ILog
    {
        /// <summary>
        ///     Property to determine if Debug logging level is enabled
        /// </summary>
        bool IsDebugEnabled { get; }

        /// <summary>
        ///     Property to determine if Error logging level is enabled
        /// </summary>
        bool IsErrorEnabled { get; }

        /// <summary>
        ///     Property to determine if Fatal logging level is enabled
        /// </summary>
        bool IsFatalEnabled { get; }

        /// <summary>
        ///     Property to determine if Info logging level is enabled
        /// </summary>
        bool IsInfoEnabled { get; }

        /// <summary>
        ///     Property to determine if Warn logging level is enabled
        /// </summary>
        bool IsWarnEnabled { get; }

        void Debug<T>(T message);

        void Debug<T>(Exception exception, T message);

        void Debug(string format, params object[] args);

        void Debug(Exception exception, string format, params object[] args);

        void Error<T>(T message);

        void Error<T>(Exception exception, T message);

        void Error(string format, params object[] args);

        void Error(Exception exception, string format, params object[] args);

        void Fatal<T>(T message);

        void Fatal<T>(Exception exception, T message);

        void Fatal(string format, params object[] args);

        void Fatal(Exception exception, string format, params object[] args);

        void Info<T>(T message);

        void Info<T>(Exception exception, T message);

        void Info(string format, params object[] args);

        void Info(Exception exception, string format, params object[] args);

        void Warn<T>(T message);

        void Warn<T>(Exception exception, T message);

        void Warn(string format, params object[] args);

        void Warn(Exception exception, string format, params object[] args);
    }
}