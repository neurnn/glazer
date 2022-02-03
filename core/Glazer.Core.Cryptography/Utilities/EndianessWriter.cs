using System;
using System.IO;
using System.Text;

namespace Glazer.Core.Cryptography.Utilities
{
    public class EndianessWriter : BinaryWriter
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
        /// Initialize a new <see cref="EndianessWriter"/> instance.
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="LittleEndian"></param>
        public EndianessWriter(Stream Output, bool LittleEndian) : this(Output, null, false, LittleEndian) { }

        /// <summary>
        /// Initialize a new <see cref="EndianessWriter"/> instance.
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="Encoding"></param>
        /// <param name="LittleEndian"></param>
        public EndianessWriter(Stream Output, Encoding Encoding, bool LittleEndian) : this(Output, Encoding, false, LittleEndian) { }

        /// <summary>
        /// Initialize a new <see cref="EndianessWriter"/> instance.
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="Encoding"></param>
        /// <param name="LeaveOpen"></param>
        /// <param name="LittleEndian"></param>
        public EndianessWriter(Stream Output, Encoding Encoding, bool LeaveOpen, bool LittleEndian) 
            : base(Output, Encoding ?? Encoding.UTF8, LeaveOpen)
        {
            if (LittleEndian != BitConverter.IsLittleEndian)
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
    }
}
