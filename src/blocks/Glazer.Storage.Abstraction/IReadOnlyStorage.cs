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
        /// Get a block asynchronously.
        /// </summary>
        /// <param name="BlockId"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Block> GetAsync(BlockId BlockId, CancellationToken Token = default);
    }
}