using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Blocks;
using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Models.Histories;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Models.Transactions;
using Glazer.Nodes.Notations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts.Trackers.Messages
{
    [NodeMessage("glazer_put_block")]
    public class PutBlock : IBinaryMessage
    {
        /// <summary>
        /// Block to put.
        /// </summary>
        public Block Block { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(Block, BlockPackingOptions.Reference);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            Block = Reader.ReadBlock(Block);
        }
    }
}
