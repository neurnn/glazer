namespace Glazer.Nodes.Models.Networks
{
    public enum NodeStatus
    {
        /// <summary>
        /// Created.
        /// </summary>
        Created,

        /// <summary>
        /// Waiting the connection is accepted or to be connected from a remote host.
        /// </summary>
        Pending,

        /// <summary>
        /// Ready to execute requests.
        /// </summary>
        Ready,

        /// <summary>
        /// Executing a request.
        /// </summary>
        Executing,
    }
}
