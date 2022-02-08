using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Blocks;
using Glazer.Nodes.Models.Histories;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Models.Transactions;
using Glazer.Nodes.Notations;
using System.IO;

namespace Glazer.Nodes.Contracts.Trackers.Messages
{
    [NodeMessage("glazer_get_row.reply")]
    public class GetRowReply : IBinaryMessage
    {
        /// <summary>
        /// History Column.
        /// </summary>
        public HistoryRow Row { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(Row);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            Row = Reader.ReadHistoryRow();
        }
    }
}
