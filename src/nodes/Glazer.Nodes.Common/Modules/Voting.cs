using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common.Internals.Synchronization;
using Glazer.Nodes.Common.Protocols;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectionVoteProtocol = Glazer.Nodes.Common.Protocols.ElectionVote;

namespace Glazer.Nodes.Common.Modules
{
    public sealed class Voting : NodeModule<Voting>
    {
        /// <inheritdoc/>
        public override int Priority => int.MinValue;

        /// <inheritdoc/>
        public override Type[] Dependencies { get; } = new Type[] { PreInit.Type };

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection Services, NodeOptions Options)
        {
            Services
                .AddSingleton<INodeElectionManager, ElectionVoteProtocol.Manager>()
                .AddSingleton<INodeSynchronizationManager, SynchronizationManager>();
        }

        /// <inheritdoc/>
        public override void ConfigureP2PMessanger(IServiceProvider App, IMessanger P2P, NodeOptions Options)
        {
            P2P.Use<ElectionVoteProtocol>(); // -> Extends the p2p messanger to implements election vote protocol.
        }
    }
}
