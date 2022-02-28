using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.IO
{
    public class AsyncBinaryReader : IDisposable
    {
        private StreamInputBuffer m_Buffer;
        private bool m_LeaveOpen;
        private Stream m_Input;

        private Func<byte[], byte[]> m_EnsureBytes = X => X;
        private Encoding m_Encoding;

        /// <summary>
        /// Initialize a new <see cref="AsyncBinaryReader"/> instance.
        /// </summary>
        /// <param name="Input"></param>
        public AsyncBinaryReader(Stream Input, Encoding Encoding, bool LeaveOpen = false, int Buffer = 4096)
        {
            m_LeaveOpen = LeaveOpen; m_Input = Input;
            m_Buffer = new StreamInputBuffer(Input.ReadAsync, Buffer);
            m_Encoding = Encoding;

            if (!BitConverter.IsLittleEndian)
                m_EnsureBytes = ReverseBytes;
        }

        /// <summary>
        /// Reverse the byte order to ensure the endianness.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        private static byte[] ReverseBytes(byte[] Input)
        {
            Array.Reverse(Input);
            return Input;
        }

        /// <summary>
        /// Read <see cref="byte"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public Task<byte> ReadByte(CancellationToken Token = default) => m_Buffer.ReadByte(Token);

        /// <summary>
        /// Read bytes with the buffering.
        /// </summary>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public Task<byte[]> ReadBytes(int Length, CancellationToken Token = default)
        {
            return m_Buffer.ReadBytes(Length, Token);
        }

        /// <summary>
        /// Read endian-aware bytes.
        /// </summary>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<byte[]> ReadEndianBytes(int Length, CancellationToken Token = default) => m_EnsureBytes(await ReadBytes(Length, Token));

        /// <summary>
        /// Read <see cref="ushort"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<ushort> ReadUInt16(CancellationToken Token = default) => BitConverter.ToUInt16(await ReadEndianBytes(sizeof(ushort), Token));

        /// <summary>
        /// Read <see cref="uint"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<uint> ReadUInt32(CancellationToken Token = default) => BitConverter.ToUInt32(await ReadEndianBytes(sizeof(uint), Token));

        /// <summary>
        /// Read <see cref="ulong"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<ulong> ReadUInt64(CancellationToken Token = default) => BitConverter.ToUInt64(await ReadEndianBytes(sizeof(ulong), Token));

        /// <summary>
        /// Read <see cref="short"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<short> ReadInt16(CancellationToken Token = default) => BitConverter.ToInt16(await ReadEndianBytes(sizeof(short), Token));

        /// <summary>
        /// Read <see cref="int"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<int> ReadInt32(CancellationToken Token = default) => BitConverter.ToInt32(await ReadEndianBytes(sizeof(int), Token));

        /// <summary>
        /// Read <see cref="long"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<long> ReadInt64(CancellationToken Token = default) => BitConverter.ToInt64(await ReadEndianBytes(sizeof(long), Token));

        /// <summary>
        /// Read <see cref="int"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<double> ReadDouble(CancellationToken Token = default) => BitConverter.ToDouble(await ReadEndianBytes(sizeof(double), Token));

        /// <summary>
        /// Read <see cref="long"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<float> ReadSingle(CancellationToken Token = default) => BitConverter.ToSingle(await ReadEndianBytes(sizeof(float), Token));

        /// <summary>
        /// Read 7bit encoded integer.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<int> Read7BitEncodedInt(CancellationToken Token = default)
        {
            int Result = 0;
            int Shifts = 0;
            byte Byte;

            Token.ThrowIfCancellationRequested();
            do
            {
                if (Shifts == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Bad 7bit encoded integer.");

                Byte = await ReadByte();
                Result |= (Byte & 0x7F) << Shifts;
                Shifts += 7;
            }

            while ((Byte & 0x80) != 0);
            return Result;
        }

        /// <summary>
        /// Read <see cref="string"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<string> ReadString(CancellationToken Token = default)
        {
            var Length = await Read7BitEncodedInt(Token);
            if (Length > 0)
            {
                var Temp = await ReadBytes(Length);
                return m_Encoding.GetString(Temp);
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (m_Buffer.DisposeAndReturnState() && !m_LeaveOpen)
            {
                m_Input?.Dispose();
                m_Input = null;
            }
        }
    }
}
