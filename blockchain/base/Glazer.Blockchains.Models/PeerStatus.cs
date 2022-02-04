namespace Glazer.Blockchains.Models
{
    public enum PeerStatus
    {
        /// <summary>
        /// Peer info instance created but, not activated.
        /// </summary>
        Created = 0,

        /// <summary>
        /// Connecting to the remote host.
        /// </summary>
        Connecting,

        /// <summary>
        /// Connection Established but, not authorized.
        /// </summary>
        Connected,

        /// <summary>
        /// Authenticating using the node's public key and login name.
        /// </summary>
        Authenticating,

        /// <summary>
        /// Authenticated.
        /// </summary>
        Authenticated,

        /// <summary>
        /// Disconnecting.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// Disconnected.
        /// </summary>
        Disconnected
    }
}
