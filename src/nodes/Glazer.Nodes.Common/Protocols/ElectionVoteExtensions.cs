using Glazer.Nodes.Abstractions;
using Glazer.P2P.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Protocols
{
    public static class ElectionVoteExtensions
    {
        /// <summary>
        /// Issue a new voting session.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Subject"></param>
        /// <param name="Data"></param>
        /// <param name="Duration"></param>
        /// <returns></returns>
        public static INodeElectionVote Issue(this IMessanger Messanger, string Subject, byte[] Data, long Duration)
            => ElectionVote.Issue(Messanger, Subject, Data, Duration);

        /// <summary>
        /// Issue a new voting session and wait its completion.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Subject"></param>
        /// <param name="Data"></param>
        /// <param name="Duration"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static Task<INodeElectionSummary> IssueAndWaitAsync(this IMessanger Messanger, string Subject, byte[] Data, long Duration, CancellationToken Token = default)
            => ElectionVote.IssueAndWaitAsync(Messanger, Subject, Data, Duration, Token);
    }
}
