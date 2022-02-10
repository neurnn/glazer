using Backrole.Core;
using Backrole.Core.Abstractions;
using Backrole.Crypto;
using Glazer.Core.Nodes.Internals;
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

namespace Glazer.Core.Nodes
{
    public class LocalNode : BackgroundService, ILocalNode
    {
        private IServiceProvider m_Services = null;
        private IBlockRepository m_BlockRepository = null;
        private ILogger m_Logger = null;

        /// <summary>
        /// Initialize a new <see cref="LocalNode"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        public LocalNode(IServiceProvider Services, IOptions<LocalNodeSettings> Options)
        {
            var Settings = Options.Value;

            m_Logger = (m_Services = Services).GetService<ILogger<ILocalNode>>();
            m_BlockRepository = m_Services.GetRequiredService<IBlockRepository>();
            m_Services.GetService<MessageMapper>().Map(typeof(LocalNode).Assembly);

            ChainId = Settings.ChainId;
            KeyPair = Settings.KeyPair;
            Account = new Account(Settings.Login, KeyPair.PublicKey);
            IsGenesisMode = Settings.GenesisMode;
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
        }

        /// <inheritdoc/>
        public NodeStatus Status => NodeStatus.Connected;

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

        /// <summary>
        /// Genesis Block.
        /// </summary>
        public Block Genesis { get; private set; }

        /// <inheritdoc/>
        public bool IsGenesisMode { get; }

        /// <inheritdoc/>
        public object GetService(Type ServiceType) => m_Services.GetService(ServiceType);

        /// <inheritdoc/>
        protected override async Task RunAsync(CancellationToken Token)
        {
            if ((Genesis = await m_BlockRepository.GetAsync(BlockIndex.Genesis, Token)) is null)
            {
                if (!IsGenesisMode)
                {
                    m_Logger.Fatal(
                        "the node has not genesis performed. please execute the node once with `--genesis` option.\n" +
                        "it will create the genesis block using your key or, synchronize the genesis block from the network.\n" +
                        "but note that, if the genesis block created by your key, it can not be interact with glazer network.\n" +
                        "to synchronize the genesis block from the network, do NOT specify `node:pvt_key' on `glnode.json` file.");

                    m_Services.GetRequiredService<IHostLifetime>().Stop();
                    return;
                }
            }
        }

    }
}
