using Backrole.Crypto;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storage.Abstraction
{
    public interface IStorage : IReadOnlyStorage
    {
        /// <summary>
        /// Put a block asynchronously.
        /// </summary>
        /// <param name="BlockId"></param>
        /// <param name="Block"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task PutAsync(BlockId BlockId, Block Block, CancellationToken Token = default);

        /// <summary>
        /// Generate a block id and then put a block asynchronously.
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<BlockId> PutAsync(Block Block, CancellationToken Token = default);
    }
}
