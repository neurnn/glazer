namespace Glazer.Core.Models
{
    public enum NodeStatus
    {
        /// <summary>
        /// Created.
        /// </summary>
        Nothing,

        /// <summary>
        /// Waiting the connection is accepted or to be connected from a remote host.
        /// </summary>
        Connecting,

        /// <summary>
        /// Ready to execute requests.
        /// </summary>
        Connected,

        /// <summary>
        /// Called when the node has been disconnected.
        /// </summary>
        Disconnected
    }
}
