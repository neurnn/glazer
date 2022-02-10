using Backrole.Core.Abstractions;
using Glazer.Core.Helpers;
using Glazer.Core.Nodes.Internals;
using Glazer.Core.Nodes.Services.Internals;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Services
{
    public class BlockRepository : IBlockRepository
    {
        private InternalBlockRepository m_LocalCopy;
        private BlockIndex m_LastBlockIndex = default;

        /// <summary>
        /// Initialize a new <see cref="BlockRepository"/> instance.
        /// </summary>
        /// <param name="Settings"></param>
        public BlockRepository(IOptions<LocalNodeSettings> Settings)
        {
            m_LocalCopy = new InternalBlockRepository(Settings);
        }

        /// <summary>
        /// Last Block Index.
        /// </summary>
        public BlockIndex LastBlockIndex => this.Locked(_ => m_LastBlockIndex);

        /// <summary>
        /// Get a block asynchronously.
        /// </summary>
        /// <param name="BlockIndex"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<Block> GetAsync(BlockIndex BlockIndex, CancellationToken Token)
        {
            var Block = await m_LocalCopy.GetAsync(BlockIndex, Token);
            if (Block is null)
            {
                if (BlockIndex == BlockIndex.Genesis)
                    return null;
                // Request to other node.
            }

            return Block;
        }

        /// <summary>
        /// Put a block asynchronously.
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<HttpStatusCode> PutAsync(Block Block, CancellationToken Token) 
            => await m_LocalCopy.PutAsync(Block, Token);
    }
}
