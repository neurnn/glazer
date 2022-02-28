using System;
using System.IO;
using System.Text;

namespace Glazer.Common
{
    public class PacketReader : BinaryReader
    {
        private Func<byte[], byte[]> m_EnsureBytes = X => X;

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
        /// Initialize a new <see cref="PacketReader"/> instance.
        /// </summary>
        /// <param name="Packet"></param>
        public PacketReader(byte[] Packet)
            : base(new MemoryStream(Packet, false), Encoding.UTF8, false)
        {
            if (!BitConverter.IsLittleEndian)
                m_EnsureBytes = ReverseBytes;
        }

        /// <summary>
        /// Initialize a new <see cref="PacketReader"/> instance.
        /// </summary>
        /// <param name="Stream"></param>
        public PacketReader(Stream Stream, bool LeaveOpen = true)
            : base(Stream, Encoding.UTF8, LeaveOpen)
        {
            if (!BitConverter.IsLittleEndian)
                m_EnsureBytes = ReverseBytes;
        }

        /// <inheritdoc/>
        public override ushort ReadUInt16() => BitConverter.ToUInt16(m_EnsureBytes(ReadBytes(sizeof(ushort))));

        /// <inheritdoc/>
        public override uint ReadUInt32() => BitConverter.ToUInt32(m_EnsureBytes(ReadBytes(sizeof(uint))));

        /// <inheritdoc/>
        public override ulong ReadUInt64() => BitConverter.ToUInt64(m_EnsureBytes(ReadBytes(sizeof(ulong))));

        /// <inheritdoc/>
        public override short ReadInt16() => BitConverter.ToInt16(m_EnsureBytes(ReadBytes(sizeof(ushort))));

        /// <inheritdoc/>
        public override int ReadInt32() => BitConverter.ToInt32(m_EnsureBytes(ReadBytes(sizeof(uint))));

        /// <inheritdoc/>
        public override long ReadInt64() => BitConverter.ToInt64(m_EnsureBytes(ReadBytes(sizeof(ulong))));

        /// <inheritdoc/>
        public override double ReadDouble() => BitConverter.ToDouble(m_EnsureBytes(ReadBytes(sizeof(double))));

        /// <inheritdoc/>
        public override float ReadSingle() => BitConverter.ToSingle(m_EnsureBytes(ReadBytes(sizeof(float))));
    }
}
