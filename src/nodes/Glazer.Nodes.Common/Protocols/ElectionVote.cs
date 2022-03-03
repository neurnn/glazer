using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common.Internals;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Protocols
{
    public sealed partial class ElectionVote : NodeMessangerProtocol
    {
        internal const string MESSAGE_TYPE = "p2p.election.vote";

        /// <summary>
        /// Initialize a new <see cref="ElectionVote"/> protocol.
        /// </summary>
        public ElectionVote() : base(MESSAGE_TYPE) { }

        /// <inheritdoc/>
        protected override Task OnMessageAsync(IServiceProvider Services, Message Message)
        {
            var Manager = Services.GetRequiredService<INodeElectionManager>();
            if (Manager is not Manager _Manager)
                return Task.CompletedTask;

            _Manager.OnMessage(Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Issue a new voting session.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Subject"></param>
        /// <param name="Data"></param>
        /// <param name="Duration"></param>
        /// <returns></returns>
        public static INodeElectionVote Issue(IMessanger Messanger, string Subject, byte[] Data, long Duration) 
            => Messanger.Services.GetRequiredService<INodeElectionManager>().Issue(Subject, Data, Duration);

        /// <summary>
        /// Issue a new voting session and wait its completion.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Subject"></param>
        /// <param name="Data"></param>
        /// <param name="Duration"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static Task<INodeElectionSummary> IssueAndWaitAsync(IMessanger Messanger, string Subject, byte[] Data, long Duration, CancellationToken Token = default)
        {
            return Issue(Messanger, Subject, Data, Duration).SummarizeAsync(Token);
        }
    }
}
