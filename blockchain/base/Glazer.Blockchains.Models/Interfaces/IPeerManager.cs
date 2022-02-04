using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IPeerManager
    {
        /// <summary>
        /// Count of connected peers.
        /// </summary>
        int Peers { get; }
        
        /// <summary>
        /// Event that broadcasts about the connected peer.
        /// </summary>
        event Action<IPeerManager, PeerInfo> Connected;

        /// <summary>
        /// Event that broadcasts about the disconnected peer.
        /// </summary>
        event Action<IPeerManager, PeerInfo> Disconnected;

        /// <summary>
        /// Event that broadcasts about the peer status changes.
        /// </summary>
        event Action<IPeerManager, PeerInfo, PeerStatus> StatusChanged;

        /// <summary>
        /// Get all peers.
        /// </summary>
        /// <returns></returns>
        IEnumerable<PeerInfo> GetPeers();

        /// <summary>
        /// Get all peers with filter.
        /// </summary>
        /// <param name="Filter"></param>
        /// <returns></returns>
        IEnumerable<PeerInfo> GetPeers(Predicate<PeerInfo> Filter);

        /// <summary>
        /// Wait the condition asynchronously.
        /// </summary>
        /// <returns></returns>
        Task WaitAsync(Func<IPeerManager, bool> Condition = null);
    }
}
