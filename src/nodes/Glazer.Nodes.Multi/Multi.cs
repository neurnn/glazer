using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common;
using Glazer.Nodes.Common.Modules;
using Glazer.Nodes.Internals;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glazer.Nodes
{
    public class Multi : NodeModule<Multi>
    {
        /// <inheritdoc/>
        public override Type[] Dependencies { get; } = new Type[] { PreInit.Type };

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection Services, NodeOptions Options)
        {
            Services
                .SetNodeEngine(NodeMode.Multi, Services => new MultiEngine(Services));
        }

        /// <inheritdoc/>
        public override void ConfigureP2PMessanger(IServiceProvider App, IMessanger P2P, NodeOptions Options)
        {
        }
    }
}
