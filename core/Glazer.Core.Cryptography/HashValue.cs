using System;

namespace Glazer.Core.Cryptography
{
    /// <summary>
    /// Hash Value.
    /// </summary>
    public struct HashValue : IEquatable<HashValue>, IEquatable<string>
    {
        /// <summary>
        /// Empty value.
        /// </summary>
        public static readonly HashValue Empty = new HashValue(null, null);

        /// <summary>
        /// Initialize a new <see cref="HashValue"/>.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <param name="Value"></param>
        public HashValue(string Algorithm, byte[] Value)
        {
            this.Algorithm = Algorithm;
            this.Value = Value;
        }

        /// <summary>
        /// Test whether two <see cref="HashValue"/>s are equal or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(HashValue Left, HashValue Right) => Left.ToString() == Right.ToString();

        /// <summary>
        /// Test whether two <see cref="HashValue"/>s are different or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(HashValue Left, HashValue Right) => Left.ToString() != Right.ToString();

        /// <summary>
        /// Parse the input string to <see cref="HashValue"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static HashValue Parse(string Input)
        {
            if (!TryParse(Input, out var Value))
                throw new InvalidOperationException("Not supported algorithm or invalid input string.");

            return Value;
        }

        /// <summary>
        /// Try to parse the input string to <see cref="HashValue"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out HashValue Output)
        {
            var Collon = Input.IndexOf(':');
            if (Collon >= 0)
            {
                var Algorithm = Input.Substring(0, Collon).ToUpper().Trim();
                var Value = Base58.Decode(Input.Substring(Collon + 1).Trim());

                Output = new HashValue(Algorithm, Value);
                return true;
            }

            if (Input.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                Output = Empty;
                return true;
            }

            Output = default;
            return false;
        }

        /// <summary>
        /// Name of the algorithm
        /// </summary>
        public string Algorithm { get; }

        /// <summary>
        /// Value of the hash.
        /// </summary>
        public byte[] Value { get; }

        /// <summary>
        /// Make the string expression for the <see cref="HashValue"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Algorithm is null || Value is null || Value.Length <= 0)
                return "null";

            return string.Join(':', Algorithm.ToUpper(), Base58.Encode(Value));
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is HashValue HV)
                return ToString() == HV.ToString();

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(HashValue Input) => ToString() == Input.ToString();

        /// <inheritdoc/>
        public bool Equals(string Input) => ToString() == Input;

        /// <inheritdoc/>
        public override int GetHashCode() => ToString().GetHashCode();
    }
}
