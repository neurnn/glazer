using Backrole.Crypto;
using Glazer.Common;

namespace Glazer.P2P.Tcp.Internals
{
    internal interface IConnection
    {
        /// <summary>
        /// Public Key of the connection.
        /// </summary>
        SignPublicKey PublicKey { get; }

        /// <summary>
        /// Emit the encoded message to the connection.
        /// </summary>
        /// <param name="Packet"></param>
        void Emit(PacketWriter Packet);
    }
}