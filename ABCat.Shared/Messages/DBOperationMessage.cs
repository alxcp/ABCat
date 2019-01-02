using System;
using System.Diagnostics;

namespace ABCat.Shared.Messages
{
    public class DBOperationMessage
    {
        public enum OperationStates
        {
            Started,
            Finished
        }

        private readonly Stopwatch _sw = new Stopwatch();

        public DBOperationMessage(OperationStates operationState)
        {
            _sw.Start();
            OperationState = operationState;
        }

        public TimeSpan Elapsed => _sw.Elapsed;

        public OperationStates OperationState { get; }
    }
}