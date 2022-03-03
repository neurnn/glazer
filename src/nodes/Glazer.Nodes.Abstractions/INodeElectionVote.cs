using Glazer.Common.Common;
using Glazer.Common.Models;
using Glazer.P2P.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeElectionVote
    {
        /// <summary>
        /// Messanger instance.
        /// </summary>
        IMessanger Messanger { get; set; }

        /// <summary>
        /// Organizer who issued the vote session.
        /// </summary>
        WitnessActor Organizer { get; }

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

        /// <summary>
        /// Submit the vote using the node itself's key-pair.
        /// </summary>
        bool Submit(byte[] Evidence);

        /// <summary>
        /// Submit the vote.
        /// </summary>
        bool Submit(WitnessActor Voter, byte[] Evidence);

        /// <summary>
        /// Wait for the voting result summary.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<INodeElectionSummary> SummarizeAsync(CancellationToken Token = default);

        /// <summary>
        /// Wait for the voting result summary.
        /// </summary>
        /// <returns></returns>
        INodeElectionSummary Summarize();
    }
}
