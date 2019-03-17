using System;

namespace SyncHole.Core.Exceptions
{
    public class OperationFailedException<T> : Exception
    {
        public T OperationContext { get; }

        public OperationFailedException(string message, T operationContext)
            : base(message)
        {
            OperationContext = operationContext;
        }

        public OperationFailedException(string message, T operationContext, Exception innerException)
        : base(message, innerException)
        {
            OperationContext = operationContext;
        }
    }
}
