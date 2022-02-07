using System;

namespace Glazer.Nodes.Exceptions
{
    /// <summary>
    /// Represents an error that the node status isn't valid to invoke something.
    /// </summary>
    public class NodeStatusException : OperationCanceledException
    {
        /// <summary>
        /// Initialize a new <see cref="NodeStatusException"/> instance.
        /// </summary>
        /// <param name="Message"></param>
        public NodeStatusException(string Message) : base(Message)
        {
        }
    }
}
