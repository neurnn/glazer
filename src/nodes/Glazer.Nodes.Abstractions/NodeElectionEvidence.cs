using Backrole.Crypto;
using Glazer.Common.Models;

namespace Glazer.Nodes.Abstractions
{
    public struct NodeElectionEvidence
    {
        /// <summary>
        /// Indicates whether the evidence is valid or not.
        /// </summary>
        public bool IsValid => Hash.IsValid;

        /// <summary>
        /// Hash of the evidence data.
        /// </summary>
        public HashValue Hash { get; set; }

        /// <summary>
        /// Data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Actors who have adopted this evidence.
        /// </summary>
        public WitnessActor[] Actors { get; set; }

        /// <summary>
        /// Percent of adoption.
        /// </summary>
        public double Percent { get; set; }
    }
}
