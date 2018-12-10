using System;

namespace ABCat.Shared
{
    public class AbCatException : Exception
    {
        public AbCatException(string message, ExceptionTypes exceptionType, Exception innerException, bool log = true)
            : base(message, innerException)
        {
            ExceptionType = exceptionType;
            Log = log;
        }

        public ExceptionTypes ExceptionType { get; }
        public bool Log { get; }
    }
}