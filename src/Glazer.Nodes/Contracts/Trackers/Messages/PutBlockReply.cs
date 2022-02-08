using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Blocks;
using Glazer.Nodes.Models.Histories;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Models.Transactions;
using Glazer.Nodes.Notations;
using Glazer.Nodes.Records;
using System.IO;
using System.Net;

namespace Glazer.Nodes.Contracts.Trackers.Messages
{
    [NodeMessage]
    public class PutBlockReply : IBinaryMessage
    {
        private static readonly HistoryColumnKey[] EMPTY_KEYS = new HistoryColumnKey[0];

        /// <summary>
        /// Status Code.
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        /// <summary>
        /// Updated Record Column Keys
        /// </summary>
        public HistoryColumnKey[] Keys { get; set; } = EMPTY_KEYS;

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            var Keys = this.Keys ?? EMPTY_KEYS;

            Writer.Write7BitEncodedInt(Keys.Length);
            foreach(var Each in Keys)
                Writer.Write(Each);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            var Count = Reader.Read7BitEncodedInt();

            Keys = new HistoryColumnKey[Count];
            for(var i = 0; i < Keys.Length; ++i)
                Keys[i] = Reader.ReadHistoryColumnKey();
        }
    }
}
