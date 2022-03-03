using Glazer.Nodes.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Internals.Engine
{
    internal class NodeEngineManager : INodeEngineManager, IDisposable
    {
        private ILogger m_Logger;

        private INodeEngine m_Engine;
        private INodeEngineFactory m_Factory;
        private IServiceProvider m_Services;

        private NodeMode m_Mode;

        // ---------------------- Running State.
        private CancellationTokenSource m_Cts;
        private Task m_Task;

        /// <summary>
        /// Initialize a new <see cref="NodeEngineManager"/> instance.
        /// </summary>
        public NodeEngineManager(ILogger<INodeEngineManager> Logger, INodeEngineFactory Factory, IServiceProvider Services)
        {
            m_Mode = NodeMode.Unknown;
            m_Logger = Logger;
            m_Factory = Factory;
            m_Services = Services;
        }

        /// <inheritdoc/>
        public INodeEngine Engine
        {
            get
            {
                lock (this)
                    return m_Engine;
            }
        }

        /// <inheritdoc/>
        public NodeMode Mode
        {
            get
            {
                lock (this)
                    return m_Mode;
            }
        }

        /// <inheritdoc/>
        public event Action<NodeMode> OnModeChanged;

        /// <inheritdoc/>
        public void SwitchTo(NodeMode Mode, bool Synchronously = false)
        {
            lock(this)
            {
                if (m_Mode == Mode)
                    return;

                m_Mode = Mode;
                SwitchTo(m_Factory.Create(Mode, m_Services), Synchronously);
            }
        }

        /// <summary>
        /// Switch the <see cref="INodeEngine"/> instance.
        /// </summary>
        /// <param name="Engine"></param>
        private void SwitchTo(INodeEngine Engine, bool Synchronously = false)
        {
            if (Synchronously)
            {
                if (this.Engine is not null)
                {
                    m_Logger.LogInformation($"Switching the node engine...");
                    Stop();
                }

                Start(Engine);
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => SwitchTo(Engine, true));
        }

        /// <summary>
        /// Stop the current running engine instance.
        /// </summary>
        private void Stop()
        {
            CancellationTokenSource Cts;
            Task Task;

            lock(this)
            {
                if ((Task = m_Task) is null)
                    return;

                m_Logger.LogInformation($"Stopping the node engine: {m_Engine.GetType().FullName}.");
                Cts = m_Cts;

                m_Task = null;
                m_Cts = null;
            }

            Cts.Cancel();
            Task.GetAwaiter().GetResult();

            Cts.Dispose();
        }

        /// <summary>
        /// Start the current 
        /// </summary>
        private void Start(INodeEngine Engine)
        {
            lock(this)
            {
                if (m_Task != null && !m_Task.IsCompleted)
                    return;

                if ((m_Engine = Engine) is null)
                    throw new InvalidOperationException("No engine instance valid.");

                m_Logger.LogInformation($"Starting the node engine: {m_Engine.GetType().FullName}.");
                m_Cts = new CancellationTokenSource();

                OnModeChanged?.Invoke(m_Mode);
                m_Task = m_Engine.RunAsync(m_Cts.Token);
            }
        }

        /// <inheritdoc/>
        public void Dispose() => Stop();
    }
}
