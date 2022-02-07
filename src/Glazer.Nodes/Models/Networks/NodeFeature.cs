namespace Glazer.Nodes.Models.Networks
{
    /// <summary>
    /// Node Abilities.
    /// </summary>
    public enum NodeFeature
    {
        /// <summary>
        /// Making concensus.
        /// Execute transactions, verifying them, then, publishes result and changes.
        /// </summary>
        Concensus = 0x01,

        /// <summary>
        /// Tracking history of records.
        /// Concensus node will sends the execution result to trackers.
        /// </summary>
        Tracker = 0x02,

        /// <summary>
        /// Storing Key-Value pairs.
        /// if Concensus node created a block, they will sends them to storage node.
        /// </summary>
        Storage = 0x04,

        /// <summary>
        /// Endpoint APIs.
        /// This makes transaction requests and read-only accesses to storage nodes.
        /// </summary>
        Endpoint = 0x08,

        /// <summary>
        /// Routing(or relaying, proxying) the requests to other nodes.
        /// Receives all requests and filters them with their own node list.
        /// </summary>
        Routing = 0x10
    }
}
