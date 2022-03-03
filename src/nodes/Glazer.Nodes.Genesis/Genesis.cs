using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common;
using Glazer.Nodes.Common.Modules;
using Glazer.Nodes.Internals;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glazer.Nodes
{
    public class Genesis : NodeModule<Genesis>
    {
        /// <inheritdoc/>
        public override int Priority => 0;

        /// <inheritdoc/>
        public override Type[] Dependencies { get; } = new Type[] { PreInit.Type };

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection Services, NodeOptions Options)
        {
            Services.SetNodeEngine(NodeMode.Genesis, Services => new GenesisEngine(Services));
        }
    }
}
