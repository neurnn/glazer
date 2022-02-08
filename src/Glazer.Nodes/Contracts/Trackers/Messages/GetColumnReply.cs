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
    [NodeMessage("glazer_get_column.reply")]
    public class GetColumnReply : IBinaryMessage
    {
        /// <summary>
        /// History Column.
        /// </summary>
        public HistoryColumn Column { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(Column);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            Column = Reader.ReadHistoryColumn();
        }
    }
}
