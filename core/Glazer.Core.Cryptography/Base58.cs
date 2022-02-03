using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Cryptography
{
    /// <summary>
    /// Base58 Encoding / Decoding
    /// </summary>
    public static class Base58
    {
        private const string DIGITS = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>
        /// Encodes data in plain Base58, without any checksum.
        /// </summary>
        /// <param name="Input">The data to be encoded</param>
        /// <returns></returns>
        public static string Encode(byte[] Input)
        {
            // Decode byte[] to BigInteger
            var Data = Input.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

            // Encode BigInteger to Base58 string
            var Result = string.Empty;
            while (Data > 0)
            {
                var remainder = (int)(Data % 58);
                Data /= 58;
                Result = DIGITS[remainder] + Result;
            }

            // Append `1` for each leading 0 byte
            for (var i = 0; i < Input.Length && Input[i] == 0; i++)
            {
                Result = '1' + Result;
            }

            return Result;
        }

        /// <summary>
        /// Decodes data in plain Base58, without any checksum.
        /// </summary>
        /// <param name="Input">Data to be decoded</param>
        /// <returns>Returns decoded data if valid; throws FormatException if invalid</returns>
        public static byte[] Decode(string Input)
        {
            // Decode Base58 string to BigInteger 
            BigInteger Data = 0;
            for (var i = 0; i < Input.Length; i++)
            {
                var Digit = DIGITS.IndexOf(Input[i]); //Slow

                if (Digit < 0)
                {
                    throw new FormatException(string.Format("Invalid Base58 character `{0}` at position {1}", Input[i], i));
                }

                Data = Data * 58 + Digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters
            var LeadingZeroCount = Input.TakeWhile(c => c == '1').Count();
            var LeadingZero = Enumerable.Repeat((byte)0, LeadingZeroCount);
            var Bytes = Data.ToByteArray()
              .Reverse()// to big endian
              .SkipWhile(b => b == 0);//strip sign byte

            return LeadingZero.Concat(Bytes).ToArray();
        }

    }
}
