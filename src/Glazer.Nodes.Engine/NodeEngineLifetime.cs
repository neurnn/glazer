using Glazer.Nodes.Engine.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Engine
{
    public class NodeEngineLifetime
    {
        private CancellationTokenSource m_Started = new();
        private CancellationTokenSource m_Stopping = new();
        private CancellationTokenSource m_Stopped = new();

        /// <summary>
        /// Initialize a new <see cref="NodeEngineLifetime"/> instance.
        /// </summary>
        /// <param name="Engine"></param>
        internal NodeEngineLifetime(NodeEngine Engine, IServiceProvider Services)
        {
            this.Engine = Engine;
            this.Services = Services;
        }

        /// <summary>
        /// Engine instance.
        /// </summary>
        public NodeEngine Engine { get; }

        /// <summary>
        /// Node Engine Service provider.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Triggered when the engine is started.
        /// </summary>
        public CancellationToken Started => m_Started.Token;

        /// <summary>
        /// Triggered when the engine is stopping.
        /// </summary>
        public CancellationToken Stopping => m_Stopping.Token;

        /// <summary>
        /// Triggered when the engine is stopped
        /// </summary>
        public CancellationToken Stopped => m_Stopped.Token;

        /// <summary>
        /// Fires the <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <param name="Cts"></param>
        /// <returns></returns>
        private bool Fire(CancellationTokenSource Cts)
        {
            lock (Cts)
            {
                if (Cts.IsCancellationRequested)
                    return false;

                Cts.Cancel();
                return true;
            }
        }

        /// <summary>
        /// Request to stop the engine.
        /// </summary>
        public void Stop()
        {
            Fire(m_Stopping);
        }

        /// <summary>
        /// Notify the engine started.
        /// </summary>
        /// <returns></returns>
        internal bool NotifyStarted() => Fire(m_Started);

        /// <summary>
        /// Notify the engine stopped.
        /// </summary>
        /// <returns></returns>
        internal bool NotifyStopped() => Fire(m_Stopped);

        /// <summary>
        /// Dispose the engine lifetime instance.
        /// </summary>
        internal void Dispose()
        {
            m_Started.Dispose();
            m_Stopping.Dispose();
            m_Stopped.Dispose();
        }
    }
}
