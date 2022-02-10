using Glazer.Core.Models.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Services
{
    /// <summary>
    /// Block Repository instance.
    /// These instances does not ENSURE the block is not modified.
    /// Just as storage.
    /// </summary>
    public interface IBlockRepository
    {
        /// <summary>
        /// Last Block Index.
        /// </summary>
        BlockIndex LastBlockIndex { get; }

        /// <summary>
        /// Get a block by its index.
        /// </summary>
        /// <param name="BlockIndex"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Block> GetAsync(BlockIndex BlockIndex, CancellationToken Token);

        /// <summary>
        /// Add a block asynchronously.
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<HttpStatusCode> PutAsync(Block Block, CancellationToken Token);
    }
}
