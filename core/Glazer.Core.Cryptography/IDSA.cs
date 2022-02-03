using System;
using System.IO;

namespace Glazer.Core.Cryptography
{
    public interface IDSA
    {
        /// <summary>
        /// Make a new <see cref="PrivateKey"/> for the DSA.
        /// </summary>
        /// <returns></returns>
        PrivateKey NewPrivateKey();

        /// <summary>
        /// Make a new <see cref="PublicKey"/> from the <see cref="PrivateKey"/>.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        PublicKey NewPublicKey(PrivateKey Key);

        /// <summary>
        /// Sign the input using the <see cref="PrivateKey"/>.
        /// </summary>
        /// <param name="PrivateKey"></param>
        /// <param name="Input"></param>
        /// <returns></returns>
        SignatureValue Sign(PrivateKey PrivateKey, ArraySegment<byte> Input);

        /// <summary>
        /// Sign the input using the <see cref="PrivateKey"/>.
        /// </summary>
        /// <param name="PrivateKey"></param>
        /// <param name="Input"></param>
        /// <returns></returns>
        SignatureValue Sign(PrivateKey PrivateKey, Stream Input);

        /// <summary>
        /// Sign the hash using the <see cref="PrivateKey"/>.
        /// </summary>
        /// <param name="PrivateKey"></param>
        /// <param name="Hash"></param>
        /// <returns></returns>
        SignatureValue Sign(PrivateKey PrivateKey, HashValue Hash);

        /// <summary>
        /// Verify the input and its signature using the specified <see cref="PublicKey"/>.
        /// </summary>
        /// <param name="Signature"></param>
        /// <param name="PublicKey"></param>
        /// <param name="Input"></param>
        /// <returns></returns>
        bool Verify(SignatureValue Signature, PublicKey PublicKey, ArraySegment<byte> Input);

        /// <summary>
        /// Verify the input and its signature using the specified <see cref="PublicKey"/>.
        /// </summary>
        /// <param name="Signature"></param>
        /// <param name="PublicKey"></param>
        /// <param name="Input"></param>
        /// <returns></returns>
        bool Verify(SignatureValue Signature, PublicKey PublicKey, Stream Input);

        /// <summary>
        /// Verify the hash and its signature using the specified <see cref="PublicKey"/>.
        /// </summary>
        /// <param name="Signature"></param>
        /// <param name="PublicKey"></param>
        /// <param name="Hash"></param>
        /// <returns></returns>
        bool Verify(SignatureValue Signature, PublicKey PublicKey, HashValue Hash);
    }
}