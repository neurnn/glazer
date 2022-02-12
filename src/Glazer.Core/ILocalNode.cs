using Backrole.Crypto;
using Glazer.Core.Models.Blocks;
using System;

namespace Glazer.Core
{
    /// <summary>
    /// Local Node interface.
    /// </summary>
    public interface ILocalNode : INode
    {
        /// <summary>
        /// Chain Id.
        /// </summary>
        Guid ChainId { get; }

        /// <summary>
        /// Key Pair to sign as configured account.
        /// </summary>
        SignKeyPair KeyPair { get; }
    }
}
