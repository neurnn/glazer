using Glazer.Core.Models.Blocks;
using Glazer.Core.Models.Interfaces;
using Glazer.Core.Models.Transactions;
using Glazer.Core.Notations;
using System.IO;

namespace Glazer.Core.Nodes.Internals.Messages
{
    [NodeMessage("get_block.reply")]
    internal class GetBlockReply : IMessage
    {
        /// <summary>
        /// Indicates whether the block exists or not.
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Block Data.
        /// </summary>
        public Block Block { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(Result ? byte.MaxValue : byte.MinValue);

            if (Result)
                Writer.WriteWithoutValidation(Block);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            Result = Reader.ReadByte() != byte.MinValue;

            if (Result)
            {
                Block = Reader.ReadBlock(Block);
            }
            else
            {
                Block = null;
            }
        }

    }
}
