using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Services
{
    /// <summary>
    /// Discovery Manager instance.
    /// </summary>
    public interface IDiscoveryManager
    {
        /// <summary>
        /// Sprinkle a seed string to the discovery manager. Supported list:<br />
        /// <code>
        /// 1. http://(IP:PORT)[/PATH] ..........<br />
        /// 2. p2p://(IP:PORT) ...........<br />
        /// 3. dns://(IP) ...........<br />
        /// </code>
        /// </summary>
        /// <param name="Seed"></param>
        /// <returns></returns>
        bool Sprinkle(string Seed);

        /// <summary>
        /// Discover other nodes from the P2P network asynchronously.
        /// </summary>
        /// <param name="TimeToLive">the maximum depth of node to which the message will be delivered.</param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IPEndPoint> DiscoverAsync(byte TimeToLive = 16, CancellationToken Token = default); 
    }
}
