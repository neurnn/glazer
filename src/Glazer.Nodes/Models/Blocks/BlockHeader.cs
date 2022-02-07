using Backrole.Crypto;
using System;

namespace Glazer.Nodes.Models.Blocks
{
    public class BlockHeader
    {
        /// <summary>
        /// Block instance.
        /// </summary>
        public Block Block { get; internal set; }

        /// <summary>
        /// Version Number.
        /// </summary>
        public uint Version { get; set; } = 0;

        /// <summary>
        /// Block Index.
        /// </summary>
        public BlockIndex Index { get; set; }

        /// <summary>
        /// TimeStamp of the block.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Previous Block Index.
        /// </summary>
        public BlockIndex PrevBlockIndex { get; set; } 

        /// <summary>
        /// Previous Block Hash.
        /// </summary>
        public HashValue PrevBlockHash { get; set; }

        /// <summary>
        /// Block Hash Value. ( Index ~ PrevBlockHash + Transactions )
        /// </summary>
        public HashValue Hash { get; set; }

        /// <summary>
        /// Block Producer.
        /// </summary>
        public Account Producer { get; set; }

        /// <summary>
        /// Block Producer's Sign.
        /// </summary>
        public SignValue ProducerSign { get; set; }
    }
}
