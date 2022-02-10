using Backrole.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Helpers
{
    public static class KeyHelpers
    {
        /// <summary>
        /// Make `<see cref="Base58"/>` key string.
        /// </summary>
        /// <param name="This"></param>
        /// <returns></returns>
        public static string ToBase58PublicKey(this SignPublicKey This)
        {
            if (This.IsValid)
                return $"PUB/{This.Value.ToBase58(true)}";

            throw new InvalidOperationException("the public key is not valid.");
        }

        /// <summary>
        /// Make `<see cref="Base58"/>` key string.
        /// </summary>
        /// <param name="This"></param>
        /// <returns></returns>
        public static string ToBase58PrivateKey(this SignPrivateKey This)
        {
            if (This.IsValid)
                return $"PVT/{This.Value.ToBase58(true)}";

            throw new InvalidOperationException("the private key is not valid.");
        }

        /// <summary>
        /// Parse `<see cref="Base58"/>` Key String.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static SignPublicKey FromBase58PublicKey(string Input)
        {
            var Slash = (Input = Input.Trim()).IndexOf('/');
            if (Slash > 0)
            {
                var Prefix = Input.Substring(0, Slash);
                var KeyVal = Base58.AsBase58(Input.Substring(Slash + 1), true);


                if (Prefix.Equals("PUB"))
                    return new SignPublicKey("SECP256K1", KeyVal);
            }

            throw new InvalidOperationException($"`{Input}` is not the public key.");
        }

        /// <summary>
        /// Parse `<see cref="Base58"/>` Key String.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static SignPrivateKey FromBase58PrivateKey(string Input)
        {
            var Slash = (Input = Input.Trim()).IndexOf('/');
            if (Slash > 0)
            {
                var Prefix = Input.Substring(0, Slash);
                var KeyVal = Base58.AsBase58(Input.Substring(Slash + 1), true);

                if (Prefix.Equals("PVT"))
                    return new SignPrivateKey("SECP256K1", KeyVal);
            }

            throw new InvalidOperationException($"`{Input}` is not the public key.");
        }
    }
}
