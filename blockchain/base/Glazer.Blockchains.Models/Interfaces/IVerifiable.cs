using Glazer.Core.Cryptography;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IVerifiable
    {
        /// <summary>
        /// Verify the instance.
        /// </summary>
        /// <returns></returns>
        VerificationStatus Verify(BlockOptions Options, bool Enforce = false);
    }
}
