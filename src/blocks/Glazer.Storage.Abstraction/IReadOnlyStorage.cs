using Backrole.Crypto;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storage.Abstraction
{
    public interface IReadOnlyStorage : IDisposable
    {
        /// <summary>
        /// Initial Block Id.
        /// </summary>
        BlockId InitialBlockId { get; }

        /// <summary>
        /// Latest Block Id.
        /// </summary>
        BlockId LatestBlockId { get; }

        /// <summary>
        /// Key-Value table that stores the surface KV set of the entire blocks.
        /// The result of replaying all blocks should be equivalent with the surface set.
        /// </summary>
        IKvTable SurfaceSet { get; }

        /// <summary>
        /// List Block IDs asynchronously.
        /// </summary>
        /// <param name="Direction">If true, from the origin to the newest, and if false, from the origin to the oldest.</param>
        /// <returns></returns>
        IAsyncEnumerable<BlockId> ListAsync(BlockId Origin, bool Direction = false, CancellationToken Token = default);

        /// <summary>
        /// Get a block asynchronously.
        /// </summary>
        /// <param name="BlockId"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Block> GetAsync(BlockId BlockId, CancellationToken Token = default);

        /// <summary>
        /// Get the hardened transaction info asynchronously.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<BlockId> GetTransactionAsync(HashValue TransactionId, CancellationToken Token = default);
    }
}