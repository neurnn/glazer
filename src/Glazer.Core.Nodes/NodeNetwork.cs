using Backrole.Core;
using Backrole.Core.Abstractions;
using Glazer.Core.Models;
using Glazer.Core.Nodes.Internals;
using Glazer.Core.Nodes.Internals.Remotes;
using Glazer.Core.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes
{
    public class NodeNetwork : IAsyncDisposable
    {
        private NodeNetworkSettings m_Options;
        private TcpListener m_Listener;

        private List<RemoteNode> m_Nodes = new();
        private HashSet<IPEndPoint> m_Endpoints = new();
        private Task m_TaskLoop;

        private ILocalNode m_LocalNode;
        private IHostLifetime m_Lifetime;
        private ILogger m_Logger;

        private List<Func<INode, object, Task<object>>> m_Handlers = new();

        /// <summary>
        /// Initialize a new <see cref="NodeNetwork"/> instance.
        /// </summary>
        /// <param name="Options"></param>
        public NodeNetwork(ILocalNode LocalNode, IOptions<NodeNetworkSettings> Options)
        {
            m_Options = Options.Value;
            m_LocalNode = LocalNode;

            m_Lifetime = m_LocalNode.GetRequiredService<IHostLifetime>();
            m_Logger = m_LocalNode.GetRequiredService<ILogger<NodeNetwork>>();
            m_Listener = new TcpListener(m_Options.LocalEndpoint); m_Listener.Start();
        }

        /// <summary>
        /// Configure<see cref="NodeNetwork"/> to <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="Services"></param>
        public static void SetServices(IServiceCollection Services) => SetServices<NodeNetwork>(Services);

        /// <summary>
        /// Configure <see cref="NodeNetwork"/> instance as <typeparamref name="TPeerNetwork"/> variation.
        /// </summary>
        /// <typeparam name="TPeerNetwork"></typeparam>
        /// <param name="Services"></param>
        public static void SetServices<TPeerNetwork>(IServiceCollection Services) where TPeerNetwork : NodeNetwork
        {
            Services
                .AddSingleton<NodeNetwork>();
        }

        /// <summary>
        /// Run the peer network asynchronously.
        /// </summary>
        internal void Run()
        {
            if (m_TaskLoop is null)
                m_TaskLoop = RunLoop();
        }

        /// <summary>
        /// Register the request listener.
        /// </summary>
        /// <param name="Listener"></param>
        public NodeNetwork ListenRequest(Func<INode, object, Task<object>> Listener)
        {
            if (m_Handlers.Contains(Listener))
                return this;

            m_Handlers.Add(Listener);
            return this;
        }

        /// <summary>
        /// Run the peer network accepter loop.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task RunLoop()
        {
            while(!m_Lifetime.Stopping.IsCancellationRequested)
            {
                TcpClient Newbie;

                try { Newbie = await m_Listener.AcceptTcpClientAsync(); }
                catch { continue; }

                Push(new RemoteNode(m_LocalNode, Newbie, m_Lifetime.Stopping));
            }
        }

        /// <summary>
        /// Push the remote node to collection.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        private void Push(RemoteNode Node, IPEndPoint Endpoint = null)
        {
            lock (this)
            {
                Node.StatusChanged += (_) =>
                {
                    if (Node.Status != NodeStatus.Disconnected)
                        return;

                    lock (this)
                    {
                        if (Endpoint != null)
                            m_Endpoints.Remove(Endpoint);

                        m_Logger.Info($"remote node disconnected, {Node.Endpoint}");

                        m_Nodes.Remove(Node);
                        Disconnected?.Invoke(Node);
                    }
                };

                m_Logger.Info($"remote node connected, {Node.Endpoint}");

                m_Nodes.Add(Node);
                Connected?.Invoke(Node);


                Node.SetListener(OnNodeRequest);
                Node.Run();
            }
        }

        /// <summary>
        /// Called when the peer network is connected.
        /// </summary>
        public event Action<INode> Connected;

        /// <summary>
        /// Called when the peer network is disconnected.
        /// </summary>
        public event Action<INode> Disconnected;

        /// <summary>
        /// Invoke an action for all nodes.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <param name="Finally"></param>
        public void Invoke(Action<INode> Delegate, Action Finally = null)
        {
            lock (this)
            {
                try
                {
                    foreach (var Each in m_Nodes)
                    {
                        Delegate(Each);
                    }
                }
                finally { Finally?.Invoke(); }
            }
        }

        /// <summary>
        /// Invoke an action for all nodes.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Delegate"></param>
        /// <param name="Finally"></param>
        /// <returns></returns>
        public Queue<TReturn> Invoke<TReturn>(Func<INode, TReturn> Delegate, Action Finally = null)
        {
            var Queue = new Queue<TReturn>();
            Invoke(Node => Queue.Enqueue(Delegate(Node)), Finally);
            return Queue;
        }

        /// <summary>
        /// Invoke an action for all nodes.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <param name="Finally"></param>
        public async Task InvokeAsync(Func<INode, Task> Delegate, Func<Task> Finally = null)
        {
            var Queue = new Queue<INode>();
            Invoke(Queue.Enqueue);

            try
            {
                while (Queue.TryDequeue(out var Each))
                    await Delegate(Each);
            }
            finally
            {
                if (Finally != null)
                    await Finally();
            }
        }

        /// <summary>
        /// Invoke an action for all nodes.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Delegate"></param>
        /// <param name="Finally"></param>
        /// <returns></returns>
        public async Task<Queue<TReturn>> InvokeAsync<TReturn>(Func<INode, Task<TReturn>> Delegate, Func<Task> Finally = null)
        {
            var Queue = new Queue<TReturn>();

            await InvokeAsync(
                async Node => Queue.Enqueue(await Delegate(Node)),
                Finally);

            return Queue;
        }

        /// <summary>
        /// Add the initial seed to network.
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <returns></returns>
        public NodeNetwork Push(IPEndPoint Endpoint, int Retry = 5)
        {
            lock (this)
            {
                if (m_Endpoints.Contains(Endpoint))
                    return this;

                m_Endpoints.Add(Endpoint);
            }

            _ = ConnectAndAdd(Endpoint, Retry);
            return this;
        }

        /// <summary>
        /// Connect and add to collection.
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <param name="Retry"></param>
        /// <returns></returns>
        private async Task ConnectAndAdd(IPEndPoint Endpoint, int Retry)
        {
            while (!m_Lifetime.Stopping.IsCancellationRequested && Retry-- > 0)
            {
                var Tcp = new TcpClient();

                try
                {
                    await Tcp.ConnectAsync(Endpoint.Address, Endpoint.Port, m_Lifetime.Stopping);
                }
                catch
                {
                    try { Tcp.Dispose(); } catch { }
                    continue;
                }

                Push(new RemoteNode(m_LocalNode, Tcp, m_Lifetime.Stopping), Endpoint);
                break;
            }
        }

        /// <summary>
        /// Wait for more remote nodes connected.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<bool> NeedMore(int Count, bool Entirely = false, CancellationToken Token = default)
        {
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Token, m_Lifetime.Stopping);
            using var Event = new AutoResetEventAsync();

            void OnConnected(INode Node) => Event.Signal();
            void OnDisconnected(INode _) => Event.Signal();

            if (Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(Count));

            var Current = 0;
            lock (this)
            {
                Current = m_Nodes.Count;

                if (!Entirely)
                    Count += Current;

                if (Current >= Count)
                    return true;

                /* Attach Events. */
                Connected += OnConnected;
                Disconnected += OnDisconnected;
            }

            try
            {
                while (!Cts.Token.IsCancellationRequested)
                {
                    lock(this)
                    {
                        if (m_Nodes.Count >= Count)
                            return true;
                    }

                    await Event.WaitAsync(Cts.Token);
                }

                return false;
            }
            finally
            {
                lock (this)
                {
                    Connected -= OnConnected;
                    Disconnected -= OnDisconnected;
                }
            }
        }

        /// <summary>
        /// Called when the node request instance comes.
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="Request"></param>
        /// <returns></returns>
        protected virtual async Task<object> OnNodeRequest(INode Node, object Request)
        {
            foreach(var Each in m_Handlers)
            {
                var Reply = await Each(Node, Request);
                if (Reply is null)
                    continue;

                return Reply;
            }

            return null;
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if (m_TaskLoop is null)
                return ValueTask.CompletedTask;

            m_Listener.Stop();
            return new ValueTask(m_TaskLoop);
        }
    }
}
