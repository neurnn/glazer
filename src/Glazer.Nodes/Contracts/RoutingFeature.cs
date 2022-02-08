using Glazer.Nodes.Models.Contracts;

namespace Glazer.Nodes.Contracts
{
    public abstract class RoutingFeature : NodeFeature
    {
        /// <summary>
        /// Routing Node.
        /// </summary>
        public override NodeFeatureType NodeType { get; } = NodeFeatureType.Routing;
    }
}
