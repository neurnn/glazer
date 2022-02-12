using Glazer.Core.Models.Blocks;
using Glazer.Core.Nodes.Internals.Messages;
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
        private RemoteNodeSender m_Sender;
        private ILocalNode m_LocalNode;

        /// <summary>
        /// Initialize a new <see cref="BlockRepository"/> instance.
        /// </summary>
        /// <param name="Sender"></param>
        public BlockRepository(ILocalNode Node, RemoteNodeSender Sender)
        {
            m_Sender = Sender;
            m_LocalNode = Node;
        }

        /// <summary>
        /// Last Block Index.
        /// </summary>
        public BlockIndex LastBlockIndex { get; private set; } = BlockIndex.Invalid;

        /// <inheritdoc/>
        public async Task<Block> GetAsync(BlockIndex BlockIndex, CancellationToken Token)
        {
            if ((await m_Sender.Request(new GetBlock { BlockIndex = BlockIndex }, Token)) is GetBlockReply Reply)
            {
                if (Reply.Result)
                {
                    LastBlockIndex 
                        = BlockIndex > LastBlockIndex
                        ? BlockIndex : LastBlockIndex;

                    return Reply.Block;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public Task<HttpStatusCode> PutAsync(Block Block, CancellationToken Token) => FORBIDDEN;
    }
}
