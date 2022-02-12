using Backrole.Core;
using Backrole.Core.Abstractions;
using Backrole.Core.Abstractions.Defaults;
using Glazer.Core.Models;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Nodes.Internals.Messages;
using Glazer.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Remotes
{
    /// <summary>
    /// Remote Node.
    /// </summary>
    internal class RemoteNode : INode, IDisposable
    {
        private Task m_Task;
        private ServiceProvider m_Services;

        private NodeStatus m_Status;
        private Account m_Account;

        private Func<INode, object, Task<object>> m_Listener;

        /// <summary>
        /// Initialize a new <see cref="TcpClient"/> instance.
        /// </summary>
        /// <param name="Tcp"></param>
        public RemoteNode(ILocalNode LocalNode, TcpClient Tcp, CancellationToken Token)
        {
            var LoggerFactory = LocalNode.GetService<ILoggerFactory>();
            var Services = new ServiceCollection()
                .AddSingleton(this, true).AddSingleton(Tcp)
                .AddSingleton(CancellationTokenSource.CreateLinkedTokenSource(Token))
                .AddSingleton<CancellationToken>(X => X.GetRequiredService<CancellationTokenSource>().Token)

                .AddSingleton<RemoteNodeSender>()

                /* Service Instances. */
                .AddSingleton<IBlockRepository, BlockRepository>()
                .AddSingleton<RemoteNodeNegotiations>()

                /* Background Services. */
                .AddHostedService<RemoteNodeReceiver>();

            /* Logging integration. */
            if (LoggerFactory != null)
                Services.AddSingleton(LoggerFactory, true);

            /* Local Node bridges. */
            Services
                .AddSingleton(LocalNode, true)
                .AddSingleton(LocalNode.GetRequiredService<MessageMapper>(), true);

            Endpoint = Tcp.Client.RemoteEndPoint as IPEndPoint;

            m_Services = new ServiceProvider(Services);
            m_Status = NodeStatus.Nothing;
        }

        /// <summary>
        /// Run the remote node's task.
        /// </summary>
        public void Run()
        {
            if (m_Task is null)
                m_Task = RunAsync();
        }

        /// <inheritdoc/>
        public NodeStatus Status => m_Status;

        /// <inheritdoc/>
        public event Action<INode> StatusChanged;

        /// <inheritdoc/>
        public IPEndPoint Endpoint { get; }

        /// <inheritdoc/>
        public Account Account => m_Account;

        /// <inheritdoc/>
        public object GetService(Type ServiceType) => m_Services.GetService(ServiceType);

        /// <summary>
        /// Set the node status.
        /// </summary>
        /// <param name="Status"></param>
        internal void SetStatus(NodeStatus Status)
        {
            lock(this)
            {
                if (Status == m_Status)
                    return;

                m_Status = Status;
            }

            ThreadPool.QueueUserWorkItem(_ => StatusChanged?.Invoke(this));
        }

        /// <summary>
        /// Set the node account.
        /// </summary>
        /// <param name="Account"></param>
        internal void SetAccount(Account Account)
        {
            lock(this)
            {
                if (Account == m_Account)
                    return;

                m_Account = Account;
            }

            ThreadPool.QueueUserWorkItem(_ => StatusChanged?.Invoke(this));
        }

        /// <summary>
        /// Set the listener delegate.
        /// </summary>
        /// <param name="Listener"></param>
        internal void SetListener(Func<INode, object, Task<object>> Listener)
        {
            m_Listener = Listener;
        }

        /// <summary>
        /// Run the remote node's receive loop.
        /// </summary>
        /// <param name="Tcp"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task RunAsync()
        {
            var Entry = m_Services.GetRequiredService<IHostedService>();
            var Token = m_Services.GetRequiredService<CancellationToken>();

            SetStatus(NodeStatus.Connecting);
            try
            {
                await Entry.StartAsync();

                var Tcs = new TaskCompletionSource();
                using (Token.Register(Tcs.SetResult))
                {
                    await m_Services
                        .GetRequiredService<RemoteNodeNegotiations>()
                        .NegotiateAsync();

                    if (!Token.IsCancellationRequested)
                        SetStatus(NodeStatus.Connected);

                    await Tcs.Task;
                }
            }
            finally
            {
                SetStatus(NodeStatus.Disconnected);
                await Entry.StopAsync();
            }
        }

        /// <summary>
        /// Called when the request message arrived.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        internal async Task<object> OnRequest(object Message)
        {
            /* Negotiation `Hello` message. */
            if (Message is Hello Hello)
            {
                return await m_Services
                    .GetRequiredService<RemoteNodeNegotiations>()
                    .OnNegotiation(Hello);
            }

            if (m_Listener != null)
                return await m_Listener(this, Message);

            return null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_Services
                .GetRequiredService<CancellationTokenSource>()
                .Cancel();

            m_Task
                .GetAwaiter()
                .GetResult();

            m_Services.Dispose();
        }

    }
}
