using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Cryptography
{
    public class Sha256 : Singleton<Sha256>, IHash
    {
        /// <inheritdoc/>
        public string Name => "SHA256";

        /// <inheritdoc/>
        public HashValue Hash(ArraySegment<byte> Input)
        {
            using (var Sha256 = SHA256.Create())
            {
                var Value = Sha256.ComputeHash(Input.Array, Input.Offset, Input.Count);
                return new HashValue(Name, Value);
            }
        }

        /// <inheritdoc/>
        public HashValue Hash(Stream Input)
        {
            using (var Sha256 = SHA256.Create())
            {
                var Value = Sha256.ComputeHash(Input);
                return new HashValue(Name, Value);
            }
        }
    }
}
