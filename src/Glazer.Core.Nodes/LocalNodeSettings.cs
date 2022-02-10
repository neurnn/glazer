using Backrole.Core;
using Backrole.Core.Abstractions;
using Backrole.Crypto;
using Glazer.Core.Exceptions;
using Glazer.Core.Helpers;
using Glazer.Core.Models;
using System;
using System.IO;
using System.Reflection;

namespace Glazer.Core.Nodes
{
    public class LocalNodeSettings
    {
        /// <summary>
        /// Chain Id.
        /// </summary>
        public Guid ChainId { get; set; }

        /// <summary>
        /// Data Directory.
        /// </summary>
        public DirectoryInfo DataDirectory { get; set; }

        /// <summary>
        /// Login Name.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Local Key Pair.
        /// </summary>
        public SignKeyPair KeyPair { get; set; }

        /// <summary>
        /// Maximum Live Partition numbers.
        /// </summary>
        public int MaxLivePartitions { get; set; } = 16;

        /// <summary>
        /// Genesis Mode.
        /// </summary>
        public bool GenesisMode { get; set; }

        /// <summary>
        /// Node Execution Arguments.
        /// </summary>
        [ServiceInjection(Required = true)]
        private IOptions<NodeArguments> m_Arguments = null;

        /// <summary>
        /// Fill the settings from <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="Configs"></param>
        /// <returns></returns>
        public LocalNodeSettings From(IConfiguration Configs)
        {
            var DataDir = Configs["node:data_dir"] ?? throw new IncompletedException($"no `data_dir` specified.");
            var PubKey58 = Configs["node:pub_key"] ?? throw new IncompletedException("no `pub_key` specified.");
            var PvtKey58 = Configs["node:pvt_key"] ?? throw new IncompletedException("no `pvt_key` specified.");

            ChainId = new Guid(Configs["node:chain_id"] ?? throw new IncompletedException($"no chain_id configured."));
            Login = Configs["node:login"] ?? throw new IncompletedException("no `login` specified.");
            KeyPair = new SignKeyPair("SECP256K1",
                KeyHelpers.FromBase58PrivateKey(PvtKey58).Value,
                KeyHelpers.FromBase58PublicKey(PubKey58).Value);

            if (DataDir.StartsWith("./"))
            {
                var BasePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                DataDir = Path.Combine(BasePath, DataDir.Substring(2));
            }

            if (!(DataDirectory = new DirectoryInfo(DataDir)).Exists)
            {
                try { Directory.CreateDirectory(DataDirectory.FullName); }
                catch
                {
                    throw new IOException($"can not create `data_dir`: {DataDir}.");
                }

                DataDirectory.Refresh();
            }

            GenesisMode = m_Arguments.Value.Contains("--genesis");
            return this;
        }
    }
}
