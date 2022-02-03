using System;

namespace Glazer.Core.Cryptography
{
    public struct Seal : IEquatable<Seal>, IEquatable<string>
    {
        /// <summary>
        /// Initialize a new <see cref="Seal"/>.
        /// </summary>
        /// <param name="PublicKey"></param>
        /// <param name="Signature"></param>
        public Seal(PublicKey PublicKey, SignatureValue Signature)
        {
            this.PublicKey = PublicKey;
            this.Signature = Signature;
        }

        /// <summary>
        /// Test whether two <see cref="Seal"/>s are equal or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(Seal Left, Seal Right) => Left.ToString() == Right.ToString();

        /// <summary>
        /// Test whether two <see cref="SignatureValue"/>s are different or not.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(Seal Left, Seal Right) => Left.ToString() != Right.ToString();

        /// <summary>
        /// Parse the input string to <see cref="Seal"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static Seal Parse(string Input)
        {
            if (!TryParse(Input, out var Value))
                throw new InvalidOperationException("Not supported algorithm or invalid input string.");

            return Value;
        }

        /// <summary>
        /// Try to parse the input string to <see cref="Seal"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out Seal Output)
        {
            var Temp = Input.Split('|', 3, StringSplitOptions.None);
            if (Temp.Length != 3)
            {
                Output = default;
                return false;
            }

            try
            {
                Output = new Seal(
                   new PublicKey(Temp[0], Base58.Decode(Temp[1])),
                   new SignatureValue(Temp[0], Base58.Decode(Temp[2])));

                return true;
            }

            catch { Output = default; }
            return false;
        }

        /// <summary>
        /// Public Key.
        /// </summary>
        public PublicKey PublicKey { get; }

        /// <summary>
        /// Signature.
        /// </summary>
        public SignatureValue Signature { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (PublicKey == PublicKey.Empty || Signature == SignatureValue.Empty)
                return "null";

            return string.Join('|', PublicKey.Algorithm,
                Base58.Encode(PublicKey.Value), Base58.Encode(Signature.Value));
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is Seal HV)
                return ToString() == HV.ToString();

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(Seal Input) => ToString() == Input.ToString();

        /// <inheritdoc/>
        public bool Equals(string Input) => ToString() == Input;

        /// <inheritdoc/>
        public override int GetHashCode() => ToString().GetHashCode();
    }
}
