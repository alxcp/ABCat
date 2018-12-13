using System;

namespace ABCat.Shared.Exceptions
{
    public class DelegateCommandCannotBeNullException : ArgumentNullException
    {
        public DelegateCommandCannotBeNullException(string paramName)
            : base(paramName, @"Delegate Command Delegates Cannot Be Null")
        {
        }
    }
}