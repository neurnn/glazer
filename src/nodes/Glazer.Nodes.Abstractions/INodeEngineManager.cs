using System;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeEngineManager
    {
        /// <summary>
        /// Node Engine instance.
        /// </summary>
        INodeEngine Engine { get; }

        /// <summary>
        /// Mode of the node engine.
        /// </summary>
        NodeMode Mode { get; }

        /// <summary>
        /// Event that notifies the <see cref="Mode"/> changes.
        /// </summary>
        event Action<NodeMode> OnModeChanged;

        /// <summary>
        /// Switch the engine mode to.
        /// </summary>
        /// <param name="Mode"></param>
        void SwitchTo(NodeMode Mode, bool Synchronously = false);
    }
}
