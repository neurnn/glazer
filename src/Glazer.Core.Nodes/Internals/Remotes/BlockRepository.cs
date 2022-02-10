using Glazer.Core.Models.Blocks;
using Glazer.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Remotes
{
    internal class BlockRepository : IBlockRepository
    {
        private static readonly Task<HttpStatusCode> FORBIDDEN = Task.FromResult(HttpStatusCode.Forbidden);

        /// <summary>
        /// Initialize a new <see cref="BlockRepository"/> instance.
        /// </summary>
        /// <param name="Node"></param>
        public BlockRepository(RemoteNode Node)
        {

        }

        public BlockIndex LastBlockIndex => throw new NotImplementedException();

        public Task<Block> GetAsync(BlockIndex BlockIndex, CancellationToken Token)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> PutAsync(Block Block, CancellationToken Token) => FORBIDDEN;
    }
}
