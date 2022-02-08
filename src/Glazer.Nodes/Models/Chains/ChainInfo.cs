using Backrole.Crypto;
using Glazer.Nodes.Models.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Glazer.Nodes.Helpers.ModelHelpers;

namespace Glazer.Nodes.Models.Chains
{
    public class ChainInfo
    {
        /// <summary>
        /// Protocol Version.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Chain that is specified manually.
        /// </summary>
        public string ChainId { get; set; }

        /// <summary>
        /// Genesis Time Stamp.
        /// </summary>
        public DateTime GenesisTimeStamp { get; set; }

        /// <summary>
        /// Genesis Key that signed the genesis block.
        /// </summary>
        public SignPublicKey GenesisKey { get; set; }

        /// <summary>
        /// Last Block's Index.
        /// </summary>
        public BlockIndex LastBlockIndex { get; set; }
        
        /// <summary>
        /// Last Block Hash.
        /// </summary>
        public HashValue LastBlockHash { get; set; }
    }
}
