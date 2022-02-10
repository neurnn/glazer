using Glazer.Core.Exceptions;
using Glazer.Core.Models;
using Glazer.Core.Networks.Internals;
using Glazer.Core.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.Core.Networks
{
    public class MeshPeer
    {
        /// <summary>
        /// Initialize a new <see cref="MeshPeer"/> instance.
        /// </summary>
        /// <param name="EndPoint"></param>
        public MeshPeer(IPEndPoint EndPoint)
        {
        }

        /// <summary>
        /// Initialize a new <see cref="MeshPeer"/> instance.
        /// </summary>
        /// <param name="Socket"></param>
        /// <param name="Messages"></param>
        internal MeshPeer(Socket Socket, MessageMapper Messages, ILocalNode LocalNode)
        {
        }

    }
}
