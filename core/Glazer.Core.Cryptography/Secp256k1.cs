using Secp256k1Net;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using S256K1 = Secp256k1Net.Secp256k1;

namespace Glazer.Core.Cryptography
{
    public class Secp256k1 : Singleton<Secp256k1>, IDSA
    {
        /// <summary>
        /// Name of the algorithm.
        /// </summary>
        public const string Name = "SECP256K1";

        /// <summary>
        /// Throw an exception if the private key is invalid.
        /// </summary>
        /// <param name="PrivateKey"></param>
        private void ThrowIfKeyInvalid(PrivateKey PrivateKey)
        {
            if (PrivateKey.Algorithm is null ||
               !PrivateKey.Algorithm.Equals(Name, StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException();

            if (PrivateKey.Value is null)
                throw new ArgumentException(nameof(PrivateKey));
        }

        /// <summary>
        /// Throw an exception if the private key is invalid.
        /// </summary>
        /// <param name="PublicKey"></param>
        private void ThrowIfKeyInvalid(PublicKey PublicKey)
        {
            if (PublicKey.Algorithm is null ||
               !PublicKey.Algorithm.Equals(Name, StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException();

            if (PublicKey.Value is null)
                throw new ArgumentException(nameof(PublicKey));
        }

        /// <inheritdoc/>
        public PrivateKey NewPrivateKey()
        {
            var PrivateKey = new byte[32];

            using (var SECP = new S256K1())
            {
                using (RandomNumberGenerator RNG = RandomNumberGenerator.Create())
                {
                    do { RNG.GetBytes(PrivateKey); }
                    while (!SECP.SecretKeyVerify(PrivateKey));
                }
            }

            return new PrivateKey(Name, PrivateKey);
        }

        /// <inheritdoc/>
        public PublicKey NewPublicKey(PrivateKey Key)
        {
            ThrowIfKeyInvalid(Key);

            var PublicKey = new byte[64];
            using (var SECP = new S256K1())
            {
                if (!SECP.PublicKeyCreate(PublicKey, Key.Value))
                    throw new ArgumentException(nameof(PrivateKey));
            }

            return new PublicKey(Name, PublicKey);
        }

        /// <inheritdoc/>
        public SignatureValue Sign(PrivateKey PrivateKey, ArraySegment<byte> Input)
        {
            ThrowIfKeyInvalid(PrivateKey);

            var HashValue = Sha256.Instance.Hash(Input);
            var Signature = new byte[64];

            using (var SECP = new S256K1())
            {
                if (!SECP.Sign(Signature, HashValue.Value, PrivateKey.Value))
                    throw new ArgumentException(nameof(PrivateKey));
            }

            return new SignatureValue(Name, Signature);
        }

        /// <inheritdoc/>
        public SignatureValue Sign(PrivateKey PrivateKey, Stream Input)
        {
            ThrowIfKeyInvalid(PrivateKey);

            var HashValue = Sha256.Instance.Hash(Input);
            var Signature = new byte[64];

            using (var SECP = new S256K1())
            {
                if (!SECP.Sign(Signature, HashValue.Value, PrivateKey.Value))
                    throw new ArgumentException(nameof(PrivateKey));
            }

            return new SignatureValue(Name, Signature);
        }

        /// <inheritdoc/>
        public SignatureValue Sign(PrivateKey PrivateKey, HashValue Hash)
            => Sign(PrivateKey, Encoding.UTF8.GetBytes(Hash.ToString()));

        /// <inheritdoc/>
        public bool Verify(SignatureValue Signature, PublicKey PublicKey, ArraySegment<byte> Input)
        {
            ThrowIfKeyInvalid(PublicKey);
            var HashValue = Sha256.Instance.Hash(Input);

            using (var SECP = new S256K1())
                return SECP.Verify(Signature.Value, HashValue.Value, PublicKey.Value);
        }

        /// <inheritdoc/>
        public bool Verify(SignatureValue Signature, PublicKey PublicKey, Stream Input)
        {
            ThrowIfKeyInvalid(PublicKey);
            var HashValue = Sha256.Instance.Hash(Input);

            using (var SECP = new S256K1())
                return SECP.Verify(Signature.Value, HashValue.Value, PublicKey.Value);
        }

        /// <inheritdoc/>
        public bool Verify(SignatureValue Signature, PublicKey PublicKey, HashValue Hash)
            => Verify(Signature, PublicKey, Encoding.UTF8.GetBytes(Hash.ToString()));
    }
}
