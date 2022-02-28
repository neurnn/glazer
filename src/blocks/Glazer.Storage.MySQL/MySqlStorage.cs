using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions;
using Glazer.Kvdb.MySQL;
using Glazer.Storage.Abstraction;
using Glazer.Storage.Abstraction.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storage.MySQL
{
    public class MySqlStorage : IStorage
    {
        private IKvScheme m_Scheme;

        private IKvTable m_Blocks;
        private IKvTable m_Properties;
        private IKvTable m_Surfaces;
        private IKvTable m_Forwards;
        private IKvTable m_Indices;

        private BlockId? m_InitialBlockId;
        private BlockId? m_LatestBlockId;

        private SurfaceTransactionIndices m_SurfaceIndices;

        /// <summary>
        /// Initialize a new <see cref="MySqlStorage"/> instance.
        /// </summary>
        public MySqlStorage(MySqlKvScheme Scheme)
        {
            m_Scheme = Scheme;

            m_Blocks = Scheme.Open("blocks") ?? Scheme.Create("blocks");
            m_Properties = Scheme.Open("properties") ?? Scheme.Create("properties");
            m_Surfaces = Scheme.Open("surfaces") ?? Scheme.Create("surfaces");
            m_Forwards = Scheme.Open("forwards") ?? Scheme.Create("forwards");
            m_Indices = Scheme.Open("indices") ?? Scheme.Create("indices");

            SurfaceSet = new SurfaceKvTable(this, m_Surfaces);
            m_SurfaceIndices = new SurfaceTransactionIndices(this, m_Indices);
        }

        /// <inheritdoc/>
        public BlockId InitialBlockId
        {
            get
            {
                lock (this)
                {
                    if (m_InitialBlockId.HasValue)
                        m_InitialBlockId = new BlockId(m_Properties.GetGuid("initial_block_id"));

                    return m_InitialBlockId.Value;
                }
            }
        }

        /// <inheritdoc/>
        public BlockId LatestBlockId
        {
            get
            {
                lock (this)
                {
                    if (m_LatestBlockId.HasValue)
                        m_LatestBlockId = new BlockId(m_Properties.GetGuid("latest_block_id"));

                    return m_InitialBlockId.Value;
                }
            }
        }

        /// <inheritdoc/>
        public IKvTable SurfaceSet { get; }

        /// <inheritdoc/>
        public async IAsyncEnumerable<BlockId> ListAsync(BlockId Origin, bool Direction = false, [EnumeratorCancellation] CancellationToken Token = default)
        {
            if (!Origin.IsValid)
                Origin = LatestBlockId;

            while (!Token.IsCancellationRequested && Origin.IsValid)
            {
                if (Direction)
                {
                    var Next = await m_Forwards.GetGuidAsync(Origin.ToString());
                    if (Next == Guid.Empty)
                        break;

                    yield return Origin = new BlockId(Next);
                }

                else
                {
                    var Block = await GetAsync(Origin);
                    if (!Block.Previous.IsValid)
                        break;

                    yield return Origin = Block.Previous.Id;
                }
            }
        }

        /// <inheritdoc/>
        public Task<BlockId> GetTransactionAsync(HashValue TransactionId, CancellationToken Token = default)
        {
            return m_SurfaceIndices.GetTransactionAsync(TransactionId, Token);
        }

        /// <inheritdoc/>
        public async Task<Block> GetAsync(BlockId BlockId, CancellationToken Token = default)
        {
            var Key = BlockId.ToString();
            var Value = await m_Blocks.GetAsync(Key, Token);

            if (Value is not null)
            {
                using var Reader = new PacketReader(Value);
                if (!Block.TryImport(Reader, out var RetVal))
                    throw new InvalidOperationException("the block storage has been corrupted.");

                return RetVal;
            }

            return default;
        }

        /// <inheritdoc/>
        public async Task PutAsync(BlockId BlockId, Block Block, CancellationToken Token = default)
        {
            var Key = BlockId.ToString();
            var NextId = await m_Forwards.GetGuidAsync(Key, Token);

            if (NextId != Guid.Empty)
                throw new InvalidOperationException("the block ID is already used.");

            using var Writer = new PacketWriter();
            if (!Block.TryExport(Writer, Block))
                throw new InvalidOperationException("the block is not valid.");

            if (!await m_Blocks.SetAsync(Key, null) ||
                !await m_Forwards.SetGuidAsync(Key, Guid.Empty) ||
                !await m_Forwards.SetGuidAsync(Block.Previous.ToString(), BlockId.Guid))
                throw new InvalidOperationException("the block couldn't be stored.");

            lock (this)
            {
                if (Block.Previous.Id == LatestBlockId)
                    m_Properties.SetGuid("latest_block_id", (m_LatestBlockId = BlockId).Value.Guid);
            }
        }

        /// <inheritdoc/>
        public async Task<BlockId> PutAsync(Block Block, CancellationToken Token = default)
        {
            while (true)
            {
                var NewId = BlockId.NewBlockId();

                var NextId = await m_Forwards.GetGuidAsync(NewId.ToString(), Token);
                if (NextId != Guid.Empty) continue;

                await PutAsync(NewId, Block, Token);
                return NewId;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_Blocks.Dispose();
            m_Forwards.Dispose();
            m_Properties.Dispose();
            m_Surfaces.Dispose();

            m_Scheme.Dispose();
        }
    }
}
