using System.Net;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface ITransport
    {
        /// <summary>
        /// Task that completed when the transport closed.
        /// </summary>
        Task Completion { get; }

        /// <summary>
        /// Peer Information of the transport.
        /// </summary>
        PeerInfo PeerInfo { get; }

        /// <summary>
        /// Send a message to the remote host.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        Task<EmitStatus> EmitAsync(object Message);

        /// <summary>
        /// Wait a message from the remote host.
        /// Note that, the waiter can be only one allowed.
        /// </summary>
        /// <returns></returns>
        Task<object> WaitAsync();
    }

}
