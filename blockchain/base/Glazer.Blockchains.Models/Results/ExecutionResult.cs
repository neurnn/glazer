using Glazer.Blockchains.Models.Interfaces;
using System;

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

        // ----------------- Determinative Properties.

        /// <summary>
        /// Indicates whether the transaction is valid or not.
        /// </summary>
        public bool IsValid => Math.Max(Agrees + Disagrees, 0) >= 3;

        /// <summary>
        /// Indicates whether the transaction is agreed finally.
        /// </summary>
        public bool IsAgreed => Agrees >= (Math.Max(Agrees + Disagrees, 1) / 3.0) * 2.0;

        /// <summary>
        /// Indicates whether the transaction is agreed finally.
        /// </summary>
        public bool IsDisagreed => !IsAgreed;
    }
}
