using System;
using System.IO;
using System.Text;

namespace Glazer.Common
{
    public class PacketWriter : BinaryWriter
    {
        private Func<byte[], byte[]> m_EnsureBytes = X => X;
        private MemoryStream m_Stream;

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
        /// Initialize a new <see cref="PacketWriter"/> instance.
        /// </summary>
        public PacketWriter() : this(out var Output) => m_Stream = Output;

        /// <summary>
        /// Initialize a new <see cref="PacketWriter"/> instance.
        /// </summary>
        /// <param name="Output"></param>
        private PacketWriter(out MemoryStream Output)
            : base(Output = new MemoryStream(), Encoding.UTF8, true)
        {
            if (!BitConverter.IsLittleEndian)
                m_EnsureBytes = ReverseBytes;
        }

        /// <inheritdoc/>
        public override void Write(ushort value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <inheritdoc/>
        public override void Write(uint value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <inheritdoc/>
        public override void Write(ulong value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <inheritdoc/>
        public override void Write(short value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <inheritdoc/>
        public override void Write(int value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <inheritdoc/>
        public override void Write(long value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <inheritdoc/>
        public override void Write(double value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <inheritdoc/>
        public override void Write(float value) => Write(m_EnsureBytes(BitConverter.GetBytes(value)));

        /// <summary>
        /// Gets the written bytes on the packet writer.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            try { Flush(); } catch { }
            return m_Stream.ToArray();
        }
    }
}
