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

        /// <summary>
        /// Genesis Block.
        /// </summary>
        Block Genesis { get; }

        /// <summary>
        /// Indicates whether the local node is running as genesis mode or not.
        /// </summary>
        bool IsGenesisMode { get; }
    }
}
