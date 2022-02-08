using Glazer.Nodes.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts
{
    public abstract class EndpointNode : NodeFeature
    {
        /// <summary>
        /// Endpoint Node.
        /// </summary>
        public override NodeFeatureType NodeType { get; } = NodeFeatureType.Endpoint;
    }
}
