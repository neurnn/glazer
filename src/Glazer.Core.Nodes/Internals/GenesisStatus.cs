using Backrole.Core.Abstractions;
using Backrole.Crypto;
using Glazer.Core.Models;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Nodes.Internals.Messages;
using Glazer.Core.Nodes.Internals.Remotes;
using Glazer.Core.Records;
using Glazer.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals
{
    internal class GenesisStatus
    {
        private LocalNodeSettings m_Settings;
        private TaskCompletionSource<Block> m_Completion = new();

        private IServiceProvider m_Services;
        private ILogger m_Logger;

        /// <summary>
        /// Initialize a new <see cref="GenesisStatus"/> instance.
        /// </summary>
        /// <param name="Options"></param>
        public GenesisStatus(IServiceProvider Services, IOptions<LocalNodeSettings> Options)
        {
            m_Settings = Options.Value;
            m_Logger = (m_Services = Services).GetRequiredService<ILogger<GenesisStatus>>();
        }

        /// <summary>
        /// Task that completed when the genesis completed.
        /// </summary>
        public Task<Block> Completion => m_Completion.Task;

        /// <summary>
        /// Initiate the genesis status instance.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task InitiateAsync(CancellationToken Token)
        {
            var Blocks = m_Services.GetRequiredService<IBlockRepository>();
            var Network = m_Services.GetRequiredService<NodeNetwork>();
            var Genesis = await Blocks.GetAsync(BlockIndex.Genesis, Token);

            try
            {
                if (Genesis is null)
                {
                    if (!m_Settings.GenesisMode)
                    {
                        m_Logger?.Fatal(
                            "the node has not genesis performed. please execute the node once with `--genesis` option.\n" +
                            "it will create the genesis block using your key or, synchronize the genesis block from the network.\n" +
                            "but note that, if the genesis block created by your key, it can not be interact with glazer network.\n" +
                            "to synchronize the genesis block from the network, specify `--genesis-sync` option.");

                        m_Services.GetRequiredService<IHostLifetime>().Stop();
                        return;
                    }

                    if (m_Settings.GenesisSyncMode)
                    {
                        var Received = await SynchronizeGenesis(Network, Token);
                        if (Received != null)
                        {
                            await Blocks.PutAsync(Received, default);
                            m_Completion.TrySetResult(Received);

                            m_Logger?.Fatal(
                                "the genesis block has been synchronized. restart without genesis options.");

                            m_Services.GetRequiredService<IHostLifetime>().Stop();
                        }

                        return;
                    }

                    else if (!m_Settings.KeyPair.PrivateKey.IsValid)
                    {
                        m_Logger?.Fatal(
                            "the node started with `--genesis` option but, " +
                            "no private key specified that is used to sign the genesis block.");

                        m_Services.GetRequiredService<IHostLifetime>().Stop();
                        return;
                    }

                    else if (m_Settings.Login != "glazer.sys")
                    {
                        m_Logger?.Fatal(
                            "the account name for `genesis` stage should be `glazer.sys`.");

                        m_Services.GetRequiredService<IHostLifetime>().Stop();
                        return;
                    }

                    var Block = new Block
                    {
                        Header = new BlockHeader
                        {
                            Version = 0,
                            TimeStamp = DateTime.UtcNow,
                            Index = BlockIndex.Genesis,
                            PrevBlockIndex = BlockIndex.Invalid,
                            PrevBlockHash = Hashes.Default.Hash("SHA256", new byte[0])
                        },

                        Records = new Dictionary<HistoryColumnKey, byte[]>
                        {
                            { 
                                new HistoryColumnKey("glazer.account", PredefinedCodeId.Account, "pub_keys"),
                                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new SignPublicKey[]
                                {
                                    m_Settings.KeyPair.PublicKey
                                }))
                            }
                        }
                    };

                    try
                    {
                        Block.Header.Hash = Block.MakeBlockHash();
                        Block.Header.Producer = new Account(m_Settings.Login, m_Settings.KeyPair.PublicKey);
                        Block.Witness.Accounts.Add(Block.Header.Producer);
                        Block.Witness.AccountSeals.Add(m_Settings.KeyPair.Sign(Block.MakeWitnessHash().Value));

                        var ProducerHash = Block.MakeProducerHash();
                        Console.WriteLine(ProducerHash);
                        Block.Header.ProducerSign = m_Settings.KeyPair.Sign(ProducerHash.Value);
                        await Blocks.PutAsync(Block, default);
                    }

                    catch { return; }
                    m_Completion.TrySetResult(Genesis = Block);

                    m_Logger?.Fatal(
                        $"the genesis block has been created successfully.\n" +
                        $"producer signature: {Block.Header.ProducerSign.Value.ToBase58()}\n" +
                        $"timestamp: {Block.Header.TimeStamp.ToString("o")}");
                }

                else if (m_Settings.GenesisMode)
                {
                    m_Logger?.Fatal(
                        "this node has genesis block, please remove `--genesis` or `--genesis-sync` options against accidential operations.");

                    m_Services.GetRequiredService<IHostLifetime>().Stop();
                    return;
                }
            }

            finally
            {
                m_Completion.TrySetResult(Genesis);
            }
        }

        /// <summary>
        /// Receive the genesis block from other nodes.
        /// </summary>
        /// <param name="Network"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<Block> SynchronizeGenesis(NodeNetwork Network, CancellationToken Token)
        {
            var ReceivedBlocks = new List<Block>();
            var SkipNodes = new HashSet<INode>();

            while (true)
            {
                var Reqeust = new GetBlock { BlockIndex = BlockIndex.Genesis };
                var Replies = await Network.InvokeAsync(async Node =>
                {
                    if (SkipNodes.Contains(Node))
                        return null;

                    SkipNodes.Add(Node);

                    m_Logger.Info($"Requesting the genesis block to {Node.Endpoint}...");
                    try
                    {
                        return (await Node
                           .GetRequiredService<RemoteNodeSender>()
                           .Request(Reqeust, Token)) as GetBlockReply;
                    }
                    catch
                    {
                        return null;
                    }
                });

                while (Replies.TryDequeue(out var Each))
                {
                    if (Each is null || !Each.Result)
                        continue;

                    ReceivedBlocks.Add(Each.Block);
                }

                if (ReceivedBlocks.Count <= 0)
                {
                    m_Logger.Info("Waiting other nodes...");
                    
                    if (!await Network.NeedMore(1, false, Token))
                        return null;

                    continue;
                }

                /* Verify the signs of the block. */
                var BlockSigns = ReceivedBlocks
                    .Select((Block, Index) =>
                    {
                        HashValue Hash;

                        try { Hash = Block.MakeProducerHash(); }
                        catch
                        {
                            return (Block, default, default);
                        }

                        return (Block, Sign: Block.Header.ProducerSign, Hash);
                    })
                    .Select(X =>
                    {
                        if (X.Sign.IsValid && X.Hash.IsValid)
                        {
                            var PublicKey = X.Block.Header.Producer.PublicKey;
                            var State = Signs.Default.Verify(PublicKey, X.Sign, X.Hash.Value);
                            return (X.Block, State: State);
                        }

                        return (X.Block, State: false);
                    });

                var Count = 0;
                var Statistics = new Dictionary<SignValue, int>();

                /* Make statistics and count the successfuls. */
                foreach (var Each in BlockSigns)
                {
                    if (!Each.State)
                    {
                        ReceivedBlocks.Remove(Each.Block);
                        continue;
                    }

                    if (!Statistics.ContainsKey(Each.Block.Header.ProducerSign))
                        Statistics[Each.Block.Header.ProducerSign] = 0;

                    Statistics[Each.Block.Header.ProducerSign]++;
                    Count++;
                }

                /* If different blocks exist, select a largest one. */
                if (Statistics.Count > 1)
                {
                    var Selected = Statistics
                        .OrderByDescending(X => X.Value)
                        .First();

                    /* remove unmatched blocks. */
                    ReceivedBlocks.RemoveAll(X => X.Header.ProducerSign != Selected.Key);
                }

                return ReceivedBlocks.First();
            }
        }
    }
}
