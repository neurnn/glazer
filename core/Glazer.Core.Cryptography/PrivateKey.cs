using System;

namespace Glazer.Core.Cryptography
{
    /// <summary>
    /// Private Key.
    /// </summary>
    public struct PrivateKey : IEquatable<PrivateKey>, IEquatable<string>
    {
        /// <summary>
        /// Empty value.
        /// </summary>
        public static readonly PrivateKey Empty = new PrivateKey(null, null);

        /// <summary>
        /// Initialize a new <see cref="PrivateKey"/>.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <param name="Value"></param>
        public PrivateKey(string Algorithm, byte[] Value)
        {
            this.Algorithm = Algorithm;
            this.Value = Value;
        }

        /// <summary>
        /// Test whether two <see cref="PrivateKey"/>s are equal or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(PrivateKey Left, PrivateKey Right) => Left.ToString() == Right.ToString();

        /// <summary>
        /// Test whether two <see cref="PrivateKey"/>s are different or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(PrivateKey Left, PrivateKey Right) => Left.ToString() != Right.ToString();

        /// <summary>
        /// Parse the input string to <see cref="PrivateKey"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static PrivateKey Parse(string Input)
        {
            if (!TryParse(Input, out var Value))
                throw new InvalidOperationException("Not supported algorithm or invalid input string.");

            return Value;
        }

        /// <summary>
        /// Try to parse the input string to <see cref="PrivateKey"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out PrivateKey Output)
        {
            var Temp = Input.Split(':', 3, StringSplitOptions.None);
            if (Temp.Length == 2)
                Temp = new[] { Temp[0], Secp256k1.Name, Temp[1] };

            if (Temp.Length == 3 && Temp[0].Equals("PVT", StringComparison.OrdinalIgnoreCase))
            {
                Output = new PrivateKey(Temp[1].ToUpper().Trim(), Base58.Decode(Temp[2]));
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
        /// Algorithm of the public key.
        /// </summary>
        public string Algorithm { get; }

        /// <summary>
        /// Value of the public key.
        /// </summary>
        public byte[] Value { get; }

        /// <summary>
        /// Make the string expression for the <see cref="PrivateKey"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Algorithm is null || Value is null || Value.Length <= 0)
                return "null";

            if (Algorithm.Equals(Secp256k1.Name, StringComparison.OrdinalIgnoreCase))
                return string.Join(':', "PUB", Base58.Encode(Value));

            return string.Join(':', "PUB", Algorithm.ToUpper(), Base58.Encode(Value));
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is PrivateKey HV)
                return ToString() == HV.ToString();

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(PrivateKey Input) => ToString() == Input.ToString();

        /// <inheritdoc/>
        public bool Equals(string Input) => ToString() == Input;

        /// <inheritdoc/>
        public override int GetHashCode() => ToString().GetHashCode();
    }
}
