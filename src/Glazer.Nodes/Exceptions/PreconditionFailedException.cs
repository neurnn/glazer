using System;

namespace Glazer.Nodes.Exceptions
{
    /// <summary>
    /// Represents an error that the target instance doesn't meet the precondition.
    /// </summary>
    public class PreconditionFailedException : OperationCanceledException
    {
        /// <summary>
        /// Initialize a new <see cref="IncompletedException"/> instance.
        /// </summary>
        /// <param name="Message"></param>
        public PreconditionFailedException(string Message) : base(Message)
        {
        }
    }
}
