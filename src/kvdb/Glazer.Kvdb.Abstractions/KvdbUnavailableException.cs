using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Abstractions
{
    public class KvdbUnavailableException : Exception
    {
        /// <summary>
        /// Initialize a new <see cref="KvdbUnavailableException"/> instance.
        /// </summary>
        public KvdbUnavailableException() : base(nameof(KvdbUnavailableException))
        {
        }

        /// <summary>
        /// Initialize a new <see cref="KvdbUnavailableException"/> instance.
        /// </summary>
        /// <param name="message"></param>
        public KvdbUnavailableException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initialize a new <see cref="KvdbUnavailableException"/> instance.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public KvdbUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
