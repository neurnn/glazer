using Glazer.Blockchains.Models.Interfaces;

namespace Glazer.Blockchains.Models.Results
{
    public class ExecutionResult
    {
        /// <summary>
        /// Nodes that executed this transaction.
        /// </summary>
        public INode[] Nodes { get; set; }

        /// <summary>
        /// Origin of the transaction.
        /// </summary>
        public PeerInfo Origin { get; set; }

        /// <summary>
        /// Information of peers involved in the execution of this transaction.
        /// This can contain nodes who are not related directly.
        /// </summary>
        public PeerInfo[] Involvators { get; set; }

        /// <summary>
        /// The number of nodes that agreed to the transaction.
        /// </summary>
        public int Agrees { get; set; }

        /// <summary>
        /// The number of nodes that disagreed to the transaction.
        /// </summary>
        public int Disagrees { get; set; }

        /// <summary>
        /// Transaction information.
        /// </summary>
        public Transaction Transaction { get; set; }
    }
}
