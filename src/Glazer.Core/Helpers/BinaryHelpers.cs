using Backrole.Crypto;
using Glazer.Core.Records;
using System;
using System.IO;

namespace Glazer.Core.Helpers
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

            var Seconds = (TimeStamp - DateTime.UnixEpoch).TotalSeconds;
            Writer.Write(Math.Ceiling(Seconds * 100) / 100);
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

        /// <summary>
        /// Read bytes from the stream.
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public static byte[] ReadBytes(this Stream Stream, int Length)
        {
            var Buffer = new byte[Length];
            var Offset = 0;

            while (Length > 0)
            {
                var Size = Stream.Read(Buffer, Offset, Length);
                if (Size <= 0)
                    throw new EndOfStreamException("end of stream");

                Offset += Size; Length -= Size;
            }

            return Buffer;
        }

        /// <summary>
        /// Read <see cref="Guid"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static Guid ReadGuid(this BinaryReader Reader)
        {
            return new Guid(Reader.ReadBytes(16));
        }

        public static void Write(this BinaryWriter Writer, Guid Guid)
        {
            Writer.Write(Guid.ToByteArray());
        }
    }
}
