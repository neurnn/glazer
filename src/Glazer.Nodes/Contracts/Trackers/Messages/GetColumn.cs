using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Blocks;
using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Models.Histories;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Models.Transactions;
using Glazer.Nodes.Notations;
using Glazer.Nodes.Records;
using System.IO;

namespace Glazer.Nodes.Contracts.Trackers.Messages
{
    [NodeMessage("glazer_get_column")]
    public class GetColumn : IBinaryMessage
    {
        /// <summary>
        /// History Column Key.
        /// </summary>
        public HistoryColumnKey ColumnKey { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(ColumnKey);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            ColumnKey = Reader.ReadHistoryColumnKey();
        }
    }
}
