using Glazer.Nodes.Abstractions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Internals.Engine
{
    internal class NodeEngineWorker : INodeEngineWorker
    {
        private Channel<Func<Task>> m_Queue;

        /// <summary>
        /// Initialize a new <see cref="NodeEngineWorker"/> instance.
        /// </summary>
        public NodeEngineWorker() => m_Queue = Channel.CreateUnbounded<Func<Task>>();

        /// <inheritdoc/>
        public ValueTask InvokeAsync(Func<Task> Delegate) => m_Queue.Writer.WriteAsync(Delegate);

        /// <summary>
        /// Service that invokes the queued delegates.
        /// </summary>
        public class Service : BackgroundService
        {
            private NodeEngineWorker m_Worker;

            /// <summary>
            /// Initialize a new <see cref="Service"/> instance.
            /// </summary>
            /// <param name="Worker"></param>
            public Service(INodeEngineWorker Worker)
            {
                if (Worker is not NodeEngineWorker _Worker)
                    throw new InvalidOperationException("the engine worker instance is not valid.");

                m_Worker = _Worker;
            }

            /// <inheritdoc/>
            protected override async Task ExecuteAsync(CancellationToken Token)
            {
                using (Token.Register(() => m_Worker.m_Queue.Writer.TryComplete()))
                {
                    while (true)
                    {
                        Func<Task> Delegate;

                        try { Delegate = await m_Worker.m_Queue.Reader.ReadAsync(); }
                        catch
                        {
                            break;
                        }

                        await Delegate();
                    }
                }
            }
        }
    }
}
