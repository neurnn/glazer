using Glazer.Common.Common;
using Glazer.Common.Models;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeElectionSummary
    {
        /// <summary>
        /// Indicates whether the vote is trustable or not.
        /// </summary>
        bool IsTrustable { get; }

        /// <summary>
        /// Indicates whether one of evidence is adoptable or not.
        /// </summary>
        bool IsAdoptable { get; }

        /// <summary>
        /// Organizer who issued the vote session.
        /// </summary>
        WitnessActor Organizer { get; }

        /// <summary>
        /// Evidences.
        /// </summary>
        NodeElectionEvidence[] Evidences { get; }

        /// <summary>
        /// Adopted Evidence.
        /// </summary>
        NodeElectionEvidence AdoptedEvidence { get; }

        /// <summary>
        /// Vote Subject.
        /// </summary>
        string Subject { get; }

        /// <summary>
        /// Expiration of the session.
        /// </summary>
        TimeStamp Expiration { get; }

        /// <summary>
        /// Data of the subject.
        /// </summary>
        byte[] Data { get; }
    }
}
