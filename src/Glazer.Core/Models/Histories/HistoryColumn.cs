using Backrole.Crypto;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Records;
using System;

namespace Glazer.Core.Models.Histories
{
    public struct HistoryColumn
    {
        /// <summary>
        /// Initialize a new <see cref="HistoryColumn"/> value.
        /// </summary>
        /// <param name="ColumnKey"></param>
        /// <param name="TimeStamp"></param>
        /// <param name="BlockIndex"></param>
        /// <param name="TrxId"></param>
        /// <param name="BlobData"></param>
        public HistoryColumn(HistoryColumnKey ColumnKey, DateTime TimeStamp,
            BlockIndex BlockIndex, HashValue TrxId, byte[] BlobData)
        {
            if (TimeStamp.Kind != DateTimeKind.Utc)
                TimeStamp = TimeStamp.ToUniversalTime();

            this.ColumnKey = ColumnKey;
            this.BlockIndex = BlockIndex;
            this.TrxId = TrxId;
            this.TimeStamp = TimeStamp;
            this.BlobData = BlobData;
        }

        /// <summary>
        /// Key that points the record itself.
        /// </summary>
        public HistoryKey RowKey => ColumnKey.RowKey;

        /// <summary>
        /// Key that points the record's column.
        /// </summary>
        public HistoryColumnKey ColumnKey { get; }

        /// <summary>
        /// Block Index that this record stored.
        /// </summary>
        public BlockIndex BlockIndex { get; }

        /// <summary>
        /// Transaction Id that made this change.
        /// </summary>
        public HashValue TrxId { get; }

        /// <summary>
        /// Time Stamp in UTC.
        /// </summary>
        public DateTime TimeStamp { get; }

        /// <summary>
        /// Blob Data of the column in this time.
        /// </summary>
        public byte[] BlobData { get; }
    }
}
