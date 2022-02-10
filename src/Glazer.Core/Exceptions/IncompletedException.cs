using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Exceptions
{
    /// <summary>
    /// Represents an error that the target instance denied the requested operation due to it is incompleted.
    /// </summary>
    public class IncompletedException : OperationCanceledException
    {
        /// <summary>
        /// Initialize a new <see cref="IncompletedException"/> instance.
        /// </summary>
        /// <param name="Message"></param>
        public IncompletedException(string Message) : base(Message)
        {
        }
    }
}
