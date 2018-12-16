

using System;
using System.Diagnostics;

namespace ABCat.Shared.Messages
{
    public class DBOperationMessage
    {
        private readonly Stopwatch _sw = new Stopwatch();

        public TimeSpan Elapsed => _sw.Elapsed;

        public DBOperationMessage(OperationStates operationState)
        {
            _sw.Start();
            OperationState = operationState;
        }

        public OperationStates OperationState { get; }

        public enum OperationStates
        {
            Started,
            Finished
        }
    }
}