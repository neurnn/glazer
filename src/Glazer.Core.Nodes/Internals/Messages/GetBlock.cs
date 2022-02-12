using Backrole.Crypto;
using Glazer.Core.Helpers;
using Glazer.Core.Models;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Models.Chains;
using Glazer.Core.Models.Histories;
using Glazer.Core.Models.Interfaces;
using Glazer.Core.Models.Transactions;
using Glazer.Core.Notations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Messages
{
    [NodeMessage("get_block")]
    internal class GetBlock : IMessage
    {
        /// <summary>
        /// Block Index to get.
        /// </summary>
        public BlockIndex BlockIndex { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(BlockIndex);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            BlockIndex = Reader.ReadBlockIndex();
        }
    }
}
