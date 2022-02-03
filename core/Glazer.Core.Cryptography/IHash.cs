using System;
using System.IO;

namespace Glazer.Core.Cryptography
{
    public interface IHash
    {
        /// <summary>
        /// Calculate the <see cref="HashValue"/> from the input bytes.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        HashValue Hash(ArraySegment<byte> Input);

        /// <summary>
        /// Calculate the <see cref="HashValue"/> from the input stream.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        HashValue Hash(Stream Input);
    }
}
