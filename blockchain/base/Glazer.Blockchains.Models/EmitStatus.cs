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
        Forbidden
    }
}
