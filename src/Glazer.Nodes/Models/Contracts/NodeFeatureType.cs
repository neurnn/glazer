namespace Glazer.Nodes.Models.Contracts
{
    /// <summary>
    /// Node Types.
    /// </summary>
    public enum NodeFeatureType
    {
        /// <summary>
        /// Making concensus, Execute transactions, verifying them, then, publishes result and changes.
        /// </summary>
        Chain = 0x01,

        /// <summary>
        /// Tracking history of records.
        /// Chain node will sends the execution result to trackers and query the latest records.
        /// </summary>
        Tracker = 0x02,

        /// <summary>
        /// Storing Key-Value pairs.
        /// if Chain node created a block, they will sends them to storage node.
        /// </summary>
        Storage = 0x04,

        /// <summary>
        /// Endpoint.
        /// These are not `LIVE` nodes. just working as client.
        /// </summary>
        Endpoint = 0x08,

        /// <summary>
        /// Routing(or relaying, proxying) the requests to other nodes.
        /// Receives all requests and filters them with their own node list.
        /// </summary>
        Routing = 0x10,

        /// <summary>
        /// Discovery that collects advertisements and provides corresponding peer information to requesters.
        /// </summary>
        Discovery = 0x20
    }
}
