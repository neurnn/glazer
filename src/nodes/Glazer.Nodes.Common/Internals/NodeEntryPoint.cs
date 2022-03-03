using Glazer.Nodes.Abstractions;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Internals
{
    internal class NodeEntryPoint : IHostedService
    {
        private NodeLifetime m_Lifetime;
        private Thread m_Thread;

        private TaskCompletionSource m_Startup = new();
        private TaskCompletionSource m_Shutdown = new();

        private INodeEngineManager m_Manager;
        private IMessanger m_Messanger;
        private NodeOptions m_Options;

        /// <summary>
        /// Initialzie a new <see cref="NodeEntryPoint"/> instance.
        /// </summary>
        /// <param name="Lifetime"></param>
        public NodeEntryPoint(
            INodeEngineManager Manager, INodeLifetime Lifetime,
            IMessanger Messanger, NodeOptions Options)
        {
            if (Lifetime is not NodeLifetime _Lifetime)
                throw new InvalidOperationException("Invalid lifetime instance configured.");

            (m_Thread = new Thread(OnMain))
                .Name = "Glazer Entry Point";

            m_Lifetime = _Lifetime;
            m_Manager = Manager;
            m_Messanger = Messanger;
            m_Options = Options;
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken Token)
        {
            m_Lifetime.NotifyStarting();
            m_Thread.Start();

            await m_Startup.Task;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken Token)
        {
            m_Lifetime.NotifyStopping();
            return m_Shutdown.Task;
        }

        /// <summary>
        /// Called to run the thread logic.
        /// </summary>
        private void OnMain()
        {
            try
            {
                var InitialMode
                    = string.IsNullOrWhiteSpace(m_Options.GenesisFile)
                    ? NodeMode.Multi : NodeMode.Genesis;

                m_Manager.SwitchTo(InitialMode, true);
                m_Startup.TrySetResult();

                /* Run the asynchronous task that dispatch the messanger's messages. */
                async Task RunAsync()
                {
                    m_Lifetime.NotifyStarted();

                    while (true)
                    {
                        try { await m_Messanger.WaitAsync(m_Lifetime.Stopping); }
                        catch
                        {
                            break;
                        }
                    }

                    m_Lifetime.NotifyStopped();
                }

                RunAsync()
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                if (m_Startup.Task.IsCompleted)
                    m_Shutdown.TrySetResult();
            }
        }

    }
}
