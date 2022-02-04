using Glazer.Core.Cryptography;
using System;
using System.Collections.Generic;
using System.Net;

namespace Glazer.Blockchains.Models
{
    public sealed class NodeOptions
    {
        /// <summary>
        /// Origin Time of the blockchain.
        /// </summary>
        public DateTime Epoch { get; set; } = new DateTime(2022, 2, 3, 10, 37, 0, DateTimeKind.Utc);

        /// <summary>
        /// Genesis Block Id.
        /// </summary>
        public Guid InitialBlockId { get; set; } = new Guid("fd4f7163-ce0b-414e-b49d-59a0e1f2c9a0");

        /// <summary>
        /// Listen Address to accept peers.
        /// </summary>
        public IPEndPoint ListenAddress { get; set; } = new IPEndPoint(IPAddress.Any, 8890);

        /// <summary>
        /// Initial Peers to connect.
        /// </summary>
        public List<IPEndPoint> InitialPeers { get; set; } = new List<IPEndPoint>();

        /// <summary>
        /// Login Name of the node.
        /// </summary>
        public string LoginName { get; set; } = "glazer";

        /// <summary>
        /// Private Key of the node itself.
        /// </summary>
        public PrivateKey NodeKey { get; set; }

        /// <summary>
        /// Public Key of the node itself.
        /// </summary>
        public PublicKey NodePublicKey { get; set; }

        /// <summary>
        /// Decides the node operating how.
        /// </summary>
        public NodeMode NodeMode { get; set; }
    }
}
