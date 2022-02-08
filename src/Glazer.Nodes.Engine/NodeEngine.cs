using Backrole.Crypto;
using Glazer.Nodes.Engine.Internals;
using Glazer.Nodes.Exceptions;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Contracts;
using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Glazer.Nodes.Helpers.ModelHelpers;

namespace Glazer.Nodes.Engine
{
    public class NodeEngine
    {
        private NodeEngineParameters m_Parameters;
        private EngineLoop m_EngineLoop;
        private Opaque m_Opaque;

        /// <summary>
        /// Initialize a new <see cref="NodeEngine"/> instance.
        /// </summary>
        public NodeEngine() => m_Opaque = new Opaque(this);

        /// <summary>
        /// Engine Parameters.
        /// </summary>
        public NodeEngineParameters Parameters
        {
            get => Ensures(ref m_Parameters);
            set => Assigns(ref m_Parameters, value);
        }

        /// <summary>
        /// Opaque class to adapt the node engine to node instance.
        /// </summary>
        private class Opaque : NodeFeature
        {
            public Opaque(NodeEngine Engine) => this.Engine = Engine;

            /// <summary>
            /// Engine Instance.
            /// </summary>
            public NodeEngine Engine { get; }

            /// <inheritdoc/>
            public override bool IsRemote => false;

            /// <inheritdoc/>
            public override bool IsRemoteInitiated => false;

            /// <inheritdoc/>
            public override NodeFeatureType NodeType { get; } = NodeFeatureType.Routing;

            /// <inheritdoc/>
            public override Account Account => Engine.Parameters.Account;

            /// <inheritdoc/>
            public void SetStatusInternal(NodeStatus Status) => SetStatus(Status);

            /// <inheritdoc/>
            public override Task<NodeResponse> ExecuteAsync(NodeRequest Request) => Engine.ExecuteAsync(Request);
        }

        /// <summary>
        /// Gets the <see cref="Node"/> instance that is opaque proxy to the <see cref="NodeEngine"/> instance.
        /// </summary>
        public NodeFeature Node => m_Opaque;

        /// <summary>
        /// Start the node engine asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public Task StartAsync()
        {
            lock(this)
            {
                if (m_EngineLoop is not null)
                    throw new InvalidOperationException("the node engine has been started.");

                m_EngineLoop = new EngineLoop(this, m_Parameters.Clone());
            }

            m_Opaque.SetStatusInternal(NodeStatus.Ready);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop the node engine asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            EngineLoop Loop;
            lock(this)
            {
                if ((Loop = m_EngineLoop) is null)
                    throw new InvalidOperationException("the node engine has been stopped.");

                m_EngineLoop = null;
            }

            m_Opaque.SetStatusInternal(NodeStatus.Pending);
            await Loop.TerminateAsync();
        }

        /// <summary>
        /// Run the engine asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken Token = default)
        {
            var Auto = this.Locked(X => X.m_EngineLoop is null);
            if (Auto)
                await StartAsync();

            try
            {
                await m_EngineLoop.WaitAsync(Token);
            }

            finally
            {
                if (Auto)
                    await StopAsync();
            }
        }

        /// <summary>
        /// Execute the node request on the engine.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public Task<NodeResponse> ExecuteAsync(NodeRequest Request)
        {
            lock (this)
            {
                if (m_EngineLoop is null)
                    throw new InvalidOperationException("the node engine has been stopped.");

                return m_EngineLoop.ExecuteAsync(Request);
            }
        }
    }

}
