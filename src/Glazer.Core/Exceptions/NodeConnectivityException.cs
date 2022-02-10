using System;

namespace Glazer.Core.Exceptions
{
    /// <summary>
    /// Represents an error that the node connectivity has problem.
    /// </summary>
    public class NodeConnectivityException : OperationCanceledException
    {
        /// <summary>
        /// Initialize a new <see cref="NodeConnectivityException"/> instance.
        /// </summary>
        /// <param name="Message"></param>
        public NodeConnectivityException(string Message) : base(Message)
        {
        }
    }
}
