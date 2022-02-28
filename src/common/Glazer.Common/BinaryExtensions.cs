using Glazer.Common;
using Glazer.Common.Common;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer
{
    public static class BinaryExtensions
    {
        /// <summary>
        /// Write <see cref="TimeStamp"/> value to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="TimeStamp"></param>
        public static void Write(this BinaryWriter Writer, TimeStamp TimeStamp) => Writer.Write7BitEncodedInt64((long)TimeStamp.Value);

        /// <summary>
        /// Read <see cref="TimeStamp"/> value from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static TimeStamp ReadTimeStamp(this BinaryReader Reader) => new TimeStamp(Reader.Read7BitEncodedInt64());

        /// <summary>
        /// Write <see cref="Guid"/> value to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Guid"></param>
        public static void Write(this BinaryWriter Writer, Guid Guid)
        {
            Writer.Write(Guid.ToByteArray());
        }

        /// <summary>
        /// Read <see cref="Guid"/> value from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static Guid ReadGuid(this BinaryReader Reader)
        {
            var Bytes = Reader.ReadBytes(16);
            if (Bytes is null || Bytes.Length != 16)
                throw new EndOfStreamException("No guid could be read.");

            return new Guid(Bytes);
        }

        /// <summary>
        /// Marshal the input bytes to <typeparamref name="T"/> structure.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T MarshalTo<T>(this byte[] Input, int Offset = 0) where T : struct
        {
            int Length;

            try { Length = Marshal.SizeOf(typeof(T)); }
            catch
            {
                throw new NotSupportedException($"{typeof(T).FullName} is not marshaling supported.");
            }

            if (Length > Input.Length)
                throw new InvalidDataException($"No {typeof(T).FullName} can be marshaled from input bytes.");

            try
            {
                GCHandle Pinned;

                if (Input.Length == Length && Offset == 0)
                    Pinned = GCHandle.Alloc(Input, GCHandleType.Pinned);

                else 
                {
                    var Temp = new byte[Length];
                    Buffer.BlockCopy(Input, Offset, Temp, 0, Length);
                    Pinned = GCHandle.Alloc(Temp, GCHandleType.Pinned);
                }

                var Result = (T)Marshal.PtrToStructure(
                    Pinned.AddrOfPinnedObject(),
                    typeof(T));

                Pinned.Free();
                return Result;
            }

            catch { }
            throw new NotSupportedException($"{typeof(T).FullName} is not marshaling supported.");
        }

        /// <summary>
        /// Marshal the input structure to byte array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static byte[] MarshalTo<T>(this T Input) where T : struct
        {
            try
            {
                var Length = Marshal.SizeOf(Input);
                var Buffer = Marshal.AllocHGlobal(Length);
                var Result = new byte[Length];

                try
                {
                    Marshal.StructureToPtr(Input, Buffer, false);
                    Marshal.Copy(Buffer, Result, 0, Length);
                }

                finally { Marshal.FreeHGlobal(Buffer); }
                return Result;
            }
            catch
            {
                throw new NotSupportedException($"{typeof(T).FullName} is not marshaling supported.");
            }
        }

        /// <summary>
        /// Read all bytes from the stream.
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<byte[]> ReadToEndAsync(this Stream Stream, CancellationToken Token = default)
        {
            using var Temp = new PacketWriter();
            var Buf = new byte[4096];

            while(true)
            {
                int Length;

                try { Length = await Stream.ReadAsync(Buf, Token); }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                    break;
                }

                if (Length <= 0)
                    break;

                Temp.Write(Buf, 0, Length);
            }

            return Temp.ToByteArray();
        }
    }
}
