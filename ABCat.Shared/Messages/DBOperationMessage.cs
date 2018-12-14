

namespace ABCat.Shared.Messages
{
    public class DBOperationMessage
    {
        public DBOperationMessage(OperationStates operationState)
        {
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