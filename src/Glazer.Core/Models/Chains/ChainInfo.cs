using Backrole.Core.Abstractions;
using Backrole.Crypto;
using Glazer.Core.Models.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Chains
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
        public Guid ChainId { get; set; }

        /// <summary>
        /// Genesis Time Stamp.
        /// </summary>
        public DateTime GenesisTimeStamp { get; set; }

        /// <summary>
        /// Genesis Key that signed the genesis block.
        /// </summary>
        public SignPublicKey GenesisKey { get; set; }
    }
}
