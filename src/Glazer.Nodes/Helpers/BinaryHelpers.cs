using Backrole.Crypto;
using Glazer.Nodes.Records;
using System;
using System.IO;

namespace Glazer.Nodes.Helpers
{
    public static class BinaryHelpers
    {
        private static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        /// Write byte frame to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        public static void WriteFrame(this BinaryWriter Writer, byte[] Bytes)
        {
            if (Bytes is null)
                Writer.Write(byte.MinValue);

            else
            {
                Writer.Write(byte.MaxValue);
                Writer.Write7BitEncodedInt(Bytes.Length);

                if (Bytes.Length > 0)
                    Writer.Write(Bytes);
            }
        }

        /// <summary>
        /// Read byte frame from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static byte[] ReadFrame(this BinaryReader Reader)
        {
            if (Reader.ReadByte() != byte.MinValue)
            {
                var Length = Reader.Read7BitEncodedInt();
                if (Length > 0)
                    return Reader.ReadBytes(Length);

                return EMPTY_BYTES;
            }

            return null;
        }

        /// <summary>
        /// Write <see cref="DateTime"/> as <see cref="DateTime.UnixEpoch"/> in seconds. (double)
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="TimeStamp"></param>
        public static void Write(this BinaryWriter Writer, DateTime TimeStamp)
        {
            if (TimeStamp.Kind != DateTimeKind.Utc)
                TimeStamp = TimeStamp.ToUniversalTime();

            Writer.Write((TimeStamp - DateTime.UnixEpoch).TotalSeconds);
        }

        /// <summary>
        /// Read <see cref="DateTime"/> as <see cref="DateTime.UnixEpoch"/> in seconds.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static DateTime ReadDateTime(this BinaryReader Reader)
        {
            return DateTime.UnixEpoch.AddSeconds(Reader.ReadDouble());
        }
    }
}
