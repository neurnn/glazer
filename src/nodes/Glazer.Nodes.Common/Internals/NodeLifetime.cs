using Glazer.Nodes.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Internals
{
    internal class NodeLifetime : INodeLifetime, IDisposable
    {
        private CancellationTokenSource
            m_Starting = new(),
            m_Started = new(),
            m_Stopping = new(),
            m_Stopped = new();

        /// <inheritdoc/>
        public CancellationToken Starting => m_Starting.Token;

        /// <inheritdoc/>
        public CancellationToken Started => m_Started.Token;

        /// <inheritdoc/>
        public CancellationToken Stopping => m_Stopping.Token;

        /// <inheritdoc/>
        public CancellationToken Stopped => m_Stopped.Token;

        /// <summary>
        /// Trigger the <see cref="CancellationTokenSource"/> immediately.
        /// </summary>
        /// <param name="Cts"></param>
        private void Trigger(CancellationTokenSource Cts)
        {
            lock (Cts)
            {
                if (Cts.IsCancellationRequested)
                    return;

                Cts.Cancel();
            }
        }

        public void NotifyStarting() => Trigger(m_Starting);

        public void NotifyStarted() => Trigger(m_Started);

        public void NotifyStopping() => Trigger(m_Stopping);

        public void NotifyStopped() => Trigger(m_Stopped);

        /// <inheritdoc/>
        public void Dispose()
        {
            m_Starting.Dispose();
            m_Started.Dispose();
            m_Stopping.Dispose();
            m_Stopped.Dispose();
        }
    }
}
