using Backrole.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes
{
    public class PeerNetwork
    {
        private PeerNetworkSettings m_Options;
        private TcpListener m_Listener;

        /// <summary>
        /// Initialize a new <see cref="PeerNetwork"/> instance.
        /// </summary>
        /// <param name="Options"></param>
        public PeerNetwork(IOptions<PeerNetworkSettings> Options)
        {
            m_Options = Options.Value;
            m_Listener = new TcpListener(m_Options.LocalEndpoint);
        }

        /// <summary>
        /// Called when the peer network is connected.
        /// </summary>
        public event Action<INode> Connected;

        /// <summary>
        /// Called when the peer network is disconnected.
        /// </summary>
        public event Action<INode> Disconnected;
    }
}
