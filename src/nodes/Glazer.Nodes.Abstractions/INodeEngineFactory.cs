using System;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeEngineFactory
    {
        /// <summary>
        /// Set the engine instance factory to the factory.
        /// </summary>
        /// <param name="Mode"></param>
        /// <param name="Engine"></param>
        /// <returns></returns>
        INodeEngineFactory Set(NodeMode Mode, Func<IServiceProvider, INodeEngine> Engine);

        /// <summary>
        /// Create an engine instance by the <see cref="NodeMode"/>.
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns></returns>
        INodeEngine Create(NodeMode Mode, IServiceProvider Services);
    }
}
