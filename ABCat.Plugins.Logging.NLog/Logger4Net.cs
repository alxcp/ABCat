using System;
using ABCat.Shared;
using NLog;

namespace ABCat.Plugins.Logging.NLog
{
    public class Logger4Net : ILog
    {
        private readonly Logger _log;

        public Logger4Net(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            _log = LogManager.GetLogger(name);
        }

        public bool IsDebugEnabled => _log.IsDebugEnabled;

        public bool IsErrorEnabled => _log.IsErrorEnabled;

        public bool IsFatalEnabled => _log.IsFatalEnabled;

        public bool IsInfoEnabled => _log.IsInfoEnabled;

        public bool IsWarnEnabled => _log.IsWarnEnabled;

        public void Debug<T>(T message)
        {
            _log.Debug(message);
        }

        public void Debug<T>(Exception exception, T message)
        {
            _log.Debug(exception, message.ToString());
        }

        public void Debug(string format, params object[] args)
        {
            _log.Debug(format.F(args));
        }

        public void Debug(Exception exception, string format, params object[] args)
        {
            try
            {
                _log.Debug(exception, string.Format(format, args));
            }
            catch (FormatException)
            {
                _log.Debug(exception, BadFormat(format, args));
            }
        }

        public void Error<T>(T message)
        {
            _log.Error(message);
        }

        public void Error<T>(Exception exception, T message)
        {
            _log.Error(exception, message.ToString());
        }

        public void Error(string format, params object[] args)
        {
            _log.Error(format.F(args));
        }

        public void Error(Exception exception, string format, params object[] args)
        {
            try
            {
                _log.Error(exception, string.Format(format, args));
            }
            catch (FormatException)
            {
                _log.Error(exception, BadFormat(format, args));
            }
        }

        public void Fatal<T>(T message)
        {
            _log.Fatal(message);
        }

        public void Fatal<T>(Exception exception, T message)
        {
            _log.Fatal(exception, message.ToString());
        }

        public void Fatal(string format, params object[] args)
        {
            _log.Fatal(format.F(args));
        }

        public void Fatal(Exception exception, string format, params object[] args)
        {
            try
            {
                _log.Fatal(exception, string.Format(format, args));
            }
            catch (FormatException)
            {
                _log.Fatal(exception, BadFormat(format, args));
            }
        }

        public void Info<T>(T message)
        {
            _log.Info(message);
        }

        public void Info<T>(Exception exception, T message)
        {
            _log.Info(exception, message.ToString());
        }

        public void Info(string format, params object[] args)
        {
            _log.Info(format.F(args));
        }

        public void Info(Exception exception, string format, params object[] args)
        {
            try
            {
                _log.Info(exception, string.Format(format, args));
            }
            catch (FormatException)
            {
                _log.Info(exception, BadFormat(format, args));
            }
        }

        public void Warn<T>(T message)
        {
            _log.Warn(message);
        }

        public void Warn<T>(Exception exception, T message)
        {
            _log.Warn(exception, message.ToString());
        }

        public void Warn(string format, params object[] args)
        {
            _log.Warn(format.F(args));
        }

        public void Warn(Exception exception, string format, params object[] args)
        {
            try
            {
                _log.Warn(exception, string.Format(format, args));
            }
            catch (FormatException)
            {
                _log.Warn(exception, BadFormat(format, args));
            }
        }

        private static string BadFormat(string format, object[] args)
        {
            var arguments = args == null ? "NULL" : string.Join("; ", Array.ConvertAll(args, a => a.ToString()));

            return $"{format}\r\nBad message format! Arguments: {arguments}";
        }
    }
}