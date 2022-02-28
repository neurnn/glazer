using Backrole.Crypto;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.P2P.Abstractions
{
    public interface IMessanger : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Use the protocol extension for the messanger.
        /// This should be invoked before waiting messages.
        /// </summary>
        /// <typeparam name="TProtocol"></typeparam>
        /// <returns></returns>
        IMessanger Use<TProtocol>() where TProtocol : IMessangerProtocol, new();

        /// <summary>
        /// Key Pair to sign the message.
        /// </summary>
        SignKeyPair KeyPair { get; }

        /// <summary>
        /// Local Endpoint.
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Event that notifies the peer entered to the local host.
        /// </summary>
        event Action<SignPublicKey> OnPeerEntered;

        /// <summary>
        /// Event that notifies the peer leaved from the local host.
        /// </summary>
        event Action<SignPublicKey> OnPeerLeaved;

        /// <summary>
        /// Test whether the target is directly connected or not.
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        bool IsConnectedDirectly(SignPublicKey Target);

        /// <summary>
        /// Gets all directly connected peers.
        /// </summary>
        /// <returns></returns>
        SignPublicKey[] GetDirectPeers();

        /// <summary>
        /// Contact to the endpoint to enlarge the connected p2p mesh.
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <returns></returns>
        IMessanger Contact(IPEndPoint Endpoint);

        /// <summary>
        /// Send a message using the messanger.
        /// </summary>
        /// <param name="Message"></param>
        IMessanger Emit(Message Message);

        /// <summary>
        /// Wait for the incoming messages from the messanger.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Message> WaitAsync(CancellationToken Token = default);
    }
}
