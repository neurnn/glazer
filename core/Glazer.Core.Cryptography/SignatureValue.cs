using System;

namespace Glazer.Core.Cryptography
{
    public struct SignatureValue : IEquatable<SignatureValue>, IEquatable<string>
    {
        /// <summary>
        /// Empty value.
        /// </summary>
        public static readonly SignatureValue Empty = new SignatureValue(null, null);

        /// <summary>
        /// Initialize a new <see cref="SignatureValue"/>.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <param name="Value"></param>
        public SignatureValue(string Algorithm, byte[] Value)
        {
            this.Algorithm = Algorithm;
            this.Value = Value;
        }

        /// <summary>
        /// Test whether two <see cref="SignatureValue"/>s are equal or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(SignatureValue Left, SignatureValue Right) => Left.ToString() == Right.ToString();

        /// <summary>
        /// Test whether two <see cref="SignatureValue"/>s are different or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(SignatureValue Left, SignatureValue Right) => Left.ToString() != Right.ToString();

        /// <summary>
        /// Parse the input string to <see cref="SignatureValue"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static SignatureValue Parse(string Input)
        {
            if (!TryParse(Input, out var Value))
                throw new InvalidOperationException("Not supported algorithm or invalid input string.");

            return Value;
        }

        /// <summary>
        /// Try to parse the input string to <see cref="SignatureValue"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out SignatureValue Output)
        {
            var Collon = Input.IndexOf(':');
            if (Collon >= 0)
            {
                var Algorithm = Input.Substring(0, Collon).ToUpper().Trim();
                var Value = Base58.Decode(Input.Substring(Collon + 1).Trim());

                Output = new SignatureValue(Algorithm, Value);
                return true;
            }

            if (Input.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                Output = Empty;
                return true;
            }

            try
            {
                var Value = Base58.Decode(Input);
                if (Value != null && Value.Length > 0)
                {
                    Output = new SignatureValue(Secp256k1.Name, Value);
                    return true;
                }
            }
            catch { }

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
        /// Make the string expression for the <see cref="SignatureValue"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Algorithm is null || Value is null || Value.Length <= 0)
                return "null";

            if (Algorithm.Equals(Secp256k1.Name, StringComparison.OrdinalIgnoreCase))
                return Base58.Encode(Value);

            return string.Join(':', Algorithm.ToUpper(), Base58.Encode(Value));
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is SignatureValue HV)
                return ToString() == HV.ToString();

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(SignatureValue Input) => ToString() == Input.ToString();

        /// <inheritdoc/>
        public bool Equals(string Input) => ToString() == Input;

        /// <inheritdoc/>
        public override int GetHashCode() => ToString().GetHashCode();
    }
}
