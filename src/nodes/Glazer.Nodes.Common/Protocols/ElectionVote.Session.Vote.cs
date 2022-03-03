using Backrole.Crypto;
using Glazer.Common.Models;

namespace Glazer.Nodes.Common.Protocols
{
    public partial class ElectionVote
    {
        internal sealed partial class Session
        {
            /// <summary>
            /// Participation.
            /// </summary>
            internal class Vote
            {
                /// <summary>
                /// Actor who participated on the vote.
                /// </summary>
                public WitnessActor Actor { get; set; }

                /// <summary>
                /// Hash to compare evidences equalities.
                /// </summary>
                public HashValue Hash { get; set; }

                /// <summary>
                /// Evidence of the vote.
                /// </summary>
                public byte[] Evidence { get; set; }
            }
        }
    }
}
