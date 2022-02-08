using System;

namespace Glazer.Nodes.Exceptions
{
    /// <summary>
    /// Represents an error that the node has no permission to execute.
    /// </summary>
    public class NodePermissionException : OperationCanceledException
    {
        /// <summary>
        /// Initialize a new <see cref="NodePermissionException"/> instance.
        /// </summary>
        /// <param name="Message"></param>
        public NodePermissionException(string Message) : base(Message)
        {
        }
    }
}
