using System;
using System.IO;
using System.Text;

namespace Glazer.Core.IO
{
    public class EndianessReader : BinaryReader
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
        /// Initialize a new <see cref="EndianessReader"/> instance.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="LittleEndian"></param>
        public EndianessReader(Stream Input, bool LittleEndian) : this(Input, null, false, LittleEndian) { }

        /// <summary>
        /// Initialize a new <see cref="EndianessReader"/> instance.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Encoding"></param>
        /// <param name="LittleEndian"></param>
        public EndianessReader(Stream Input, Encoding Encoding, bool LittleEndian) : this(Input, Encoding, false, LittleEndian) { }

        /// <summary>
        /// Initialize a new <see cref="EndianessReader"/> instance.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Encoding"></param>
        /// <param name="LeaveOpen"></param>
        /// <param name="LittleEndian"></param>
        public EndianessReader(Stream Input, Encoding Encoding, bool LeaveOpen, bool LittleEndian)
            : base(Input, Encoding ?? Encoding.UTF8, LeaveOpen)
        {
            if (LittleEndian != BitConverter.IsLittleEndian)
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
