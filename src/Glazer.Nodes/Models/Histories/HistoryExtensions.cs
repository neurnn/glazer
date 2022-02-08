using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models.Blocks;
using Glazer.Nodes.Models.Histories;
using Glazer.Nodes.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Models.Histories
{
    public static class HistoryExtensions
    {
        /// <summary>
        /// Write <see cref="RecordCoRecordKeylumnKey"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="RecordKey"></param>
        public static void Write(this BinaryWriter Writer, HistoryRowKey RecordKey)
        {
            if (RecordKey.IsNull)
                Writer.Write(HashValue.Empty);

            else
            {
                Writer.Write(RecordKey.CodeId);
                Writer.Write(RecordKey.Login);
            }
        }

        /// <summary>
        /// Read <see cref="HistoryRowKey"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static HistoryRowKey ReadHistoryRowKey(this BinaryReader Reader)
        {
            var CodeId = Reader.ReadHashValue();
            if (CodeId.IsValid)
            {
                var Login = Reader.ReadString();
                return new HistoryRowKey(Login, CodeId);
            }

            return HistoryRowKey.Null;
        }

        /// <summary>
        /// Write <see cref="HistoryColumnKey"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="RecordKey"></param>
        public static void Write(this BinaryWriter Writer, HistoryColumnKey RecordKey)
        {
            if (RecordKey.IsNull)
                Writer.Write(HashValue.Empty);

            else
            {
                Writer.Write(RecordKey.CodeId);
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
            var CodeId = Reader.ReadHashValue();
            if (CodeId.IsValid)
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

        /// <summary>
        /// Write <see cref="HistoryRow"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Column"></param>
        public static void Write(this BinaryWriter Writer, HistoryRow Row)
        {
            Writer.Write(Row.Key);

            if (!Row.Key.IsNull)
            {
                if (Row.Columns is null)
                    Writer.Write7BitEncodedInt(0);

                else
                {
                    Writer.Write7BitEncodedInt(Row.Columns.Length);
                    foreach(var Each in Row.Columns)
                    {
                        Writer.Write(Each.ColumnKey.Column);
                        Writer.Write(Each.BlockIndex);
                        Writer.Write(Each.TrxId);
                        Writer.Write(Each.TimeStamp);
                        Writer.WriteFrame(Each.BlobData);
                    }
                }
            }
        }

        /// <summary>
        /// Read <see cref="HistoryRow"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static HistoryRow ReadHistoryRow(this BinaryReader Reader)
        {
            var Key = Reader.ReadHistoryRowKey();
            if (Key.IsNull)
                return default;

            var Count = Reader.Read7BitEncodedInt();
            var Columns = new HistoryColumn[Count];

            for (var i = 0; i < Count; ++i)
            {
                var ColumnKey = new HistoryColumnKey(Key, Reader.ReadString());
                var BlockIndex = Reader.ReadBlockIndex();
                var TrxId = Reader.ReadHashValue();
                var TimeStamp = Reader.ReadDateTime();
                var BlobData = Reader.ReadFrame();
                Columns[i] = new HistoryColumn(ColumnKey, TimeStamp, BlockIndex, TrxId, BlobData);
            }

            return new HistoryRow(Key, Columns);
        }
    }
}
