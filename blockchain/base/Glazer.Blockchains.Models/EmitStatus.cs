namespace Glazer.Blockchains.Models
{
    public enum EmitStatus
    {
        /// <summary>
        /// Delivered successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Not connected yet.
        /// </summary>
        NotConnected,

        /// <summary>
        /// Disconnected with the peer.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Forbidden by the remote peer.
        /// </summary>
        Forbidden,

        /// <summary>
        /// Failed by the transport.
        /// </summary>
        Failure,

        /// <summary>
        /// Failed to invoke the packet callbacks.
        /// </summary>
        InvalidPacket
    }
}
