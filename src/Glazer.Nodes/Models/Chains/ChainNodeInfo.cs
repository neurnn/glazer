using Backrole.Crypto;
using Glazer.Nodes.Models.Contracts;
using System;

namespace Glazer.Nodes.Models.Chains
{
    public class ChainNodeInfo
    {
        /// <summary>
        /// Time Stamp when this node encoded this.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Chain Information.
        /// </summary>
        public ChainInfo Chain { get; set; }

        /// <summary>
        /// Account that this node introduces.
        /// </summary>
        public Account Account { get; set; }

        /// <summary>
        /// Role of the node.
        /// </summary>
        public NodeFeatureType Feature { get; set; }

        /// <summary>
        /// Signature.
        /// </summary>
        public SignValue Signature { get; set; }
    }
}
