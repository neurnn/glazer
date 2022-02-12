using Backrole.Core;
using Backrole.Core.Abstractions;
using Backrole.Crypto;
using Glazer.Core.Nodes.Internals.Messages;
using Glazer.Core.Nodes.Services;
using Glazer.Core.Nodes.Services.Internals;
using Glazer.Core.Models;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Services;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Backrole.Core.Hosting;
using Glazer.Core.Nodes.Internals;
using System.Linq;
using System.Reflection;
using Glazer.Core.Notations;

namespace Glazer.Core.Nodes
{
    public class LocalNode : BackgroundService, ILocalNode
    {
        private NodeStatus m_Status;
        private IServiceProvider m_Services = null;
        private ILogger m_Logger = null;

        /// <summary>
        /// Initialize a new <see cref="LocalNode"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        public LocalNode(IServiceProvider Services, IOptions<LocalNodeSettings> Options)
        {
            var Settings = Options.Value;

            m_Logger = (m_Services = Services).GetService<ILogger<ILocalNode>>();
            m_Services.GetService<MessageMapper>()
                .Map(
                    typeof(LocalNode).Assembly.GetTypes()
                        .Where(X => X.GetCustomAttribute<NodeMessageAttribute>() != null)
                        .ToArray()
                );

            ChainId = Settings.ChainId;
            KeyPair = Settings.KeyPair;
            Account = new Account(Settings.Login, KeyPair.PublicKey);
        }

        /// <summary>
        /// Configure<see cref="LocalNode"/> to <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="Services"></param>
        public static void SetServices(IServiceCollection Services) => SetServices<LocalNode>(Services);

        /// <summary>
        /// Configure <see cref="LocalNode"/> instance as <typeparamref name="TLocalNode"/> variation.
        /// </summary>
        /// <typeparam name="TLocalNode"></typeparam>
        /// <param name="Services"></param>
        public static void SetServices<TLocalNode>(IServiceCollection Services) where TLocalNode : LocalNode
        {
            Services.AddSingleton<ILocalNode, TLocalNode>();
            Services /* Entry Point. */
                .AddHostedService(X => (TLocalNode) X.GetService<ILocalNode>());

            if (Services.Find<MessageMapper>() is null)
                Services.AddSingleton<MessageMapper>();

            if (Services.Find<IBlockRepository>() is null)
                Services.AddSingleton<IBlockRepository, BlockRepository>();

            Services.AddHostedService<DiscoveryService>();
            Services.AddSingleton<GenesisStatus>();
        }

        /// <inheritdoc/>
        public NodeStatus Status => m_Status;

        /// <inheritdoc/>
        public event Action<INode> StatusChanged; // --> unused.

        /// <inheritdoc/>
        public IPEndPoint Endpoint { get; }

        /// <inheritdoc/>
        public Guid ChainId { get; }

        /// <inheritdoc/>
        public Account Account { get; }

        /// <inheritdoc/>
        public SignKeyPair KeyPair { get; }

        /// <inheritdoc/>
        public object GetService(Type ServiceType) => m_Services.GetService(ServiceType);

        /// <summary>
        /// Set the node status.
        /// </summary>
        /// <param name="Status"></param>
        internal void SetStatus(NodeStatus Status)
        {
            lock (this)
            {
                if (Status == m_Status)
                    return;

                m_Status = Status;
            }

            ThreadPool.QueueUserWorkItem(_ => StatusChanged?.Invoke(this));
        }

        /// <inheritdoc/>
        protected override async Task RunAsync(CancellationToken Token)
        {
            var Network = m_Services.GetRequiredService<NodeNetwork>();
            var Status = m_Services.GetRequiredService<GenesisStatus>();

            SetStatus(NodeStatus.Connecting);
            Network
                .ListenRequest(OnRequest)
                .Run();

            await Status.InitiateAsync(Token);
            try
            {

            }

            finally
            {
                SetStatus(NodeStatus.Disconnected);
            }
        }

        /// <summary>
        /// Called when the node received the request.
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="Request"></param>
        /// <returns></returns>
        private async Task<object> OnRequest(INode Node, object Message)
        {
            /* GetBlock request about genesis is allowed even under negotiation. */
            if (Message is GetBlock GetBlock && GetBlock.BlockIndex == BlockIndex.Genesis)
            {
                var Token = Node.GetRequiredService<CancellationToken>();
                var LocalNode = m_Services.GetRequiredService<ILocalNode>();
                var Genesis = m_Services.GetRequiredService<GenesisStatus>();

                var Tcs = new TaskCompletionSource();
                using (Token.Register(Tcs.SetResult))
                {
                    await Task.WhenAny(Genesis.Completion, Tcs.Task);
                    if (Tcs.Task.IsCompleted)
                        return null;

                    var Block = await Genesis.Completion;
                    return new GetBlockReply
                    {
                        Block = Block,
                        Result = Block != null
                    };
                }
            }

            return null;
        }
    }
}
