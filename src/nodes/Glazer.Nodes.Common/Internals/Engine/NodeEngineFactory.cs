using Glazer.Nodes.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Internals.Engine
{
    internal class NodeEngineFactory : INodeEngineFactory
    {
        private bool m_Initiated = false;
        private Dictionary<NodeMode, Func<IServiceProvider, INodeEngine>> m_Factories = new();

        /// <inheritdoc/>
        public INodeEngineFactory Set(NodeMode Mode, Func<IServiceProvider, INodeEngine> Engine)
        {
            if (m_Initiated)
                throw new InvalidOperationException("No `Set` allowed for the running engine factory.");

            m_Factories[Mode] = Engine;
            return this;
        }

        /// <inheritdoc/>
        public INodeEngine Create(NodeMode Mode, IServiceProvider Services)
        {
            m_Initiated = true;
            m_Factories.TryGetValue(Mode, out var Factory);

            if (Factory is null)
                throw new InvalidOperationException("Not supported mode.");

            return Factory(Services);
        }

    }
}
