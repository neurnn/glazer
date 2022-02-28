using Glazer.Accessors;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions;
using Glazer.Storage.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Glazer.Executions.Abstractions.ExecutionStatus;

namespace Glazer.Executions.Abstractions
{
    public sealed class ExecutionContextFactory
    {
        private Dictionary<string, Func<ExecutionContext>> m_Factories = new();
        private ProtocolAccessor m_Protocols;
        private IStorage m_Storage;

        /// <summary>
        /// Initialize a new <see cref="ExecutionContextFactory"/> instance.
        /// </summary>
        /// <param name="Storage"></param>
        public ExecutionContextFactory(IStorage Storage)
        {
            m_Protocols = new ProtocolAccessor(Storage.SurfaceSet);
            m_Storage = Storage;
        }

        /// <summary>
        /// Set the execution context factory.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public ExecutionContextFactory SetFactory(string Type, Func<ExecutionContext> Factory)
        {
            lock(m_Factories)
            {
                m_Factories[Type] = Factory;
                return this;
            }
        }

        /// <summary>
        /// Create a new <see cref="ExecutionContext"/> instance for the given <see cref="Actor"/> and <see cref="ScriptAction"/>.
        /// </summary>
        /// <param name="AuthorizedActor"></param>
        /// <param name="Action"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<ExecutionContext> CreateAsync(Actor AuthorizedActor, ScriptAction Action, CancellationToken Token = default)
        {
            Func< ExecutionContext> Factory;

            var Context = GetCurrentContext();
            if (Context != null && Context.AuthorizedActor != AuthorizedActor)
                throw new InvalidOperationException("No authorization switching allowed.");

            var Protocol = await m_Protocols.GetAsync(Action.Script, Token);
            if (!Protocol.IsValid)
                throw new InvalidOperationException("No contract found.");

            lock (m_Factories)
            {
                if (!m_Factories.TryGetValue(Protocol.Type, out Factory) || Factory is null)
                    throw new NotSupportedException($"No execution context supported for: {Protocol.Type}.");
            }

            PushCurrentParameters(new ExecutionConstructorParameters
            {
                AuthorizedActor = AuthorizedActor,
                TargetAction = Action, Contract = Protocol, Factory = this,
                ScopedKvTable = m_Storage.SurfaceSet.DisableDispose().Prefix($"{Action.Script}:")
            });

            try { return Factory(); }
            finally { PopCurrentParameters(); }
        }
    }
}
