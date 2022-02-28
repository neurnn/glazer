using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.P2P.Tcp.Internals
{
    internal class ContactBook : HashSet<IPEndPoint>
    {
        /// <summary>
        /// Set the endpoint that is now contacting...
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <returns></returns>
        public bool Set(IPEndPoint Endpoint)
        {
            lock (this)
            {
                return Add(Endpoint);
            }
        }

        /// <summary>
        /// Unset the endpoint from the contact book.
        /// </summary>
        /// <param name="Endpoint"></param>
        public void Unset(IPEndPoint Endpoint)
        {
            lock (this)
            {
                Remove(Endpoint);
            }
        }
    }
}
