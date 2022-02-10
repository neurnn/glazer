using Backrole.Crypto;
using Glazer.Core.Helpers;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Models.Histories;
using Glazer.Core.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Models.Histories
{
    public static class HistoryExtensions
    {
        /// <summary>
        /// Write <see cref="RecordCoRecordKeylumnKey"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="RecordKey"></param>
        public static void Write(this BinaryWriter Writer, HistoryKey RecordKey)
        {
            if (RecordKey.IsNull)
                Writer.Write(Guid.Empty.ToByteArray());

            else
            {
                Writer.Write(RecordKey.CodeId.ToByteArray());
                Writer.Write(RecordKey.Login);
            }
        }

        /// <summary>
        /// Read <see cref="HistoryKey"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static HistoryKey ReadHistoryRowKey(this BinaryReader Reader)
        {
            var CodeId = new Guid(Reader.ReadBytes(16));
            if (CodeId != Guid.Empty)
            {
                var Login = Reader.ReadString();
                return new HistoryKey(Login, CodeId);
            }

            return HistoryKey.Null;
        }

        /// <summary>
        /// Write <see cref="HistoryColumnKey"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="RecordKey"></param>
        public static void Write(this BinaryWriter Writer, HistoryColumnKey RecordKey)
        {
            if (RecordKey.IsNull)
                Writer.Write(Guid.Empty.ToByteArray());

            else
            {
                Writer.Write(RecordKey.CodeId.ToByteArray());
                Writer.Write(RecordKey.Login);
                Writer.Write(RecordKey.Column);
            }
        }

        /// <summary>
        /// Read <see cref="HistoryColumnKey"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static HistoryColumnKey ReadHistoryColumnKey(this BinaryReader Reader)
        {
            var CodeId = new Guid(Reader.ReadBytes(16));
            if (CodeId != Guid.Empty)
            {
                var Login = Reader.ReadString();
                var Column = Reader.ReadString();
                return new HistoryColumnKey(Login, CodeId, Column);
            }

            return HistoryColumnKey.Null;
        }

        /// <summary>
        /// Write <see cref="HistoryColumn"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Column"></param>
        public static void Write(this BinaryWriter Writer, HistoryColumn Column)
        {
            Writer.Write(Column.ColumnKey);

            if (!Column.ColumnKey.IsNull)
            {
                Writer.Write(Column.BlockIndex);
                Writer.Write(Column.TrxId);
                Writer.Write(Column.TimeStamp);
                Writer.WriteFrame(Column.BlobData);
            }
        }

        /// <summary>
        /// Read <see cref="HistoryColumnKey"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static HistoryColumn ReadHistoryColumn(this BinaryReader Reader)
        {
            var ColumnKey = Reader.ReadHistoryColumnKey();
            if (ColumnKey.IsNull)
                return default;

            var BlockIndex = Reader.ReadBlockIndex();
            var TrxId = Reader.ReadHashValue();
            var TimeStamp = Reader.ReadDateTime();
            var BlobData = Reader.ReadFrame();

            return new HistoryColumn(ColumnKey, TimeStamp, BlockIndex, TrxId, BlobData);
        }

    }
}
