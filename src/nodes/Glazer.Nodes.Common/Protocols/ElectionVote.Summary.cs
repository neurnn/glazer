using Backrole.Crypto;
using Glazer.Common.Common;
using Glazer.Common.Models;
using Glazer.Nodes.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Glazer.Nodes.Common.Protocols
{
    public partial class ElectionVote
    {
        internal sealed class Summary : INodeElectionSummary
        {
            /// <summary>
            /// Initialize a new <see cref="Summary"/> instance.
            /// </summary>
            /// <param name="Votes"></param>
            internal Summary(Session Session, IEnumerable<Session.Vote> Votes)
            {
                Organizer = Session.Organizer;
                Subject = Session.Subject;
                Expiration = Session.Expiration;
                Data = Session.Data;

                var TotalVotes = Votes.Count();
                Evidences = Categorize(Votes)
                    .Select(Hash =>
                    {
                        var Actors = Votes
                            .Where(Y => Y.Hash == Hash)
                            .Select(Y => Y.Actor)
                            .ToArray();

                        var Data = Votes.First(Y => Y.Hash == Hash);
                        return new NodeElectionEvidence
                        {
                            Hash = Hash, Data = Data.Evidence, Actors = Actors,
                            Percent = (Actors.Length * 1.0 / TotalVotes) * 100.0
                        };
                    })
                    .OrderByDescending(X => X.Percent)
                    .ToArray();

                IsTrustable = Evidences.Length >= 1 && TotalVotes >= 3;
                IsAdoptable = TotalVotes > 0 && DetermineAdoptable(Evidences);
                AdoptedEvidence = IsAdoptable ? Evidences.First() : default;
            }

            /// <summary>
            /// Determines whether one of evidence is adoptable or not.
            /// </summary>
            private static bool DetermineAdoptable(NodeElectionEvidence[] Evidences)
            {
                // --> When all voters submitted same evidence.
                if (Evidences.Length == 1)
                    return true;

                // --> Checks if the first of evidence has more witness than the others.
                if (Evidences[1].Actors.Length < Evidences[0].Actors.Length)
                    return true;

                return false;
            }

            /// <summary>
            /// Categorize the votes as the evidence hash.
            /// </summary>
            /// <param name="Votes"></param>
            /// <returns></returns>
            private static List<HashValue> Categorize(IEnumerable<Session.Vote> Votes)
            {
                var Categories = new List<HashValue>();
                foreach (var Vote in Votes)
                {
                    if (Categories.FindIndex(X => X == Vote.Hash) < 0)
                        Categories.Add(Vote.Hash);
                }

                return Categories;
            }

            /// <summary>
            /// Indicates whether the vote is trustable or not.
            /// </summary>
            public bool IsTrustable { get; }

            /// <summary>
            /// Indicates whether one of evidence is adoptable or not.
            /// </summary>
            public bool IsAdoptable { get; }

            /// <summary>
            /// Organizer who issued the vote session.
            /// </summary>
            public WitnessActor Organizer { get; }

            /// <summary>
            /// Evidences.
            /// </summary>
            public NodeElectionEvidence[] Evidences { get; }

            /// <summary>
            /// Adopted Evidence.
            /// </summary>
            public NodeElectionEvidence AdoptedEvidence { get; }

            /// <summary>
            /// Vote Subject.
            /// </summary>
            public string Subject { get; }

            /// <summary>
            /// Expiration of the session.
            /// </summary>
            public TimeStamp Expiration { get; }

            /// <summary>
            /// Data of the subject.
            /// </summary>
            public byte[] Data { get; }

        }
    }
}
