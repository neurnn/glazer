using Glazer.Blockchains.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    /// <summary>
    /// Saves blocks or Loads saved blocks.
    /// </summary>
    public interface IBlockRepository
    {
        /// <summary>
        /// Test whether the repository is read-only or not.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Load a block from the repository asynchronously.
        /// </summary>
        /// <param name="Guid"></param>
        /// <returns></returns>
        Task<Block> ReadAsync(Guid Guid, CancellationToken Token = default);

        /// <summary>
        /// Save a block to the repository asynchronously.
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Block> WriteAsync(Block Block, CancellationToken Token = default);
    }

}
