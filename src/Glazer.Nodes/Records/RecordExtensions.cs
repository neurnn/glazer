using Backrole.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Records
{
    public static class RecordExtensions
    {
        /// <summary>
        /// Write <see cref="RecordCoRecordKeylumnKey"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="RecordKey"></param>
        public static void Write(this BinaryWriter Writer, RecordKey RecordKey)
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
        /// Read <see cref="RecordKey"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static RecordKey ReadRecordKey(this BinaryReader Reader)
        {
            var CodeId = Reader.ReadHashValue();
            if (CodeId.IsValid)
            {
                var Login = Reader.ReadString();
                return new RecordKey(Login, CodeId);
            }

            return RecordKey.Null;
        }

        /// <summary>
        /// Write <see cref="RecordColumnKey"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="RecordKey"></param>
        public static void Write(this BinaryWriter Writer, RecordColumnKey RecordKey)
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
        /// Read <see cref="RecordColumnKey"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static RecordColumnKey ReadRecordColumnKey(this BinaryReader Reader)
        {
            var CodeId = Reader.ReadHashValue();
            if (CodeId.IsValid)
            {
                var Login = Reader.ReadString();
                var Column = Reader.ReadString();
                return new RecordColumnKey(Login, CodeId, Column);
            }

            return RecordColumnKey.Null;
        }
    }
}
