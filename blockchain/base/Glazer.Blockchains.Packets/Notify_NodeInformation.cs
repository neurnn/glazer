using Glazer.Blockchains.Models;
using Glazer.Blockchains.Models.Interfaces;
using Glazer.Core.Cryptography;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Glazer.Blockchains.Packets
{
    public class Notify_NodeInformation
    {
        /// <summary>
        /// Create the node information instance from the <see cref="NodeOptions"/> instance.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static Notify_NodeInformation FromOptions(NodeOptions Options)
        {
            return new Notify_NodeInformation
            {
                Epoch = Options.Epoch,
                ChainId = Options.ChainId,
                InitialBlockId = Options.InitialBlockId,
                NodeLoginName = Options.LoginName,
                NodePubKey = Options.NodePublicKey,
                NodeMode = Options.NodeMode
            };
        }

        /// <summary>
        /// Epoch Value.
        /// </summary>
        [JsonProperty("epoch")]
        public string EpochString { get; set; } = DateTime.UtcNow.ToString("o");
        [JsonIgnore]
        public DateTime Epoch
        {
            get => DateTime.ParseExact(EpochString, "o", CultureInfo.InvariantCulture);
            set => EpochString = value.ToString("o");
        }

        /// <summary>
        /// Chain Id.
        /// </summary>
        [JsonProperty("chain_id")]
        public string ChainIdString { get; set; } = Guid.Empty.ToString();
        [JsonIgnore]
        public Guid ChainId
        {
            get => new Guid(ChainIdString);
            set => ChainIdString = value.ToString();
        }

        /// <summary>
        /// Initial Block Id.
        /// </summary>
        [JsonProperty("initial_block_id")]
        public string InitialBlockIdString { get; set; } = Guid.Empty.ToString();
        [JsonIgnore]
        public Guid InitialBlockId
        {
            get => new Guid(InitialBlockIdString);
            set => InitialBlockIdString = value.ToString();
        }

        /// <summary>
        /// Node Id.
        /// </summary>
        [JsonProperty("node_login")]
        public string NodeLoginName { get; set; }

        /// <summary>
        /// Node Public Key.
        /// </summary>
        [JsonProperty("node_pub_key")]
        public string NodePubKeyString { get; set; } = PublicKey.Empty.ToString();
        [JsonIgnore]
        public PublicKey NodePubKey
        {
            get => PublicKey.Parse(NodePubKeyString);
            set => NodePubKeyString = value.ToString();
        }

        /// <summary>
        /// Node Mode.
        /// </summary>
        [JsonProperty("node_mode")]
        public NodeMode NodeMode { get; set; } = NodeMode.Plain;
    }
}
