using Glazer.Common;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions;
using Glazer.Kvdb.Sqlite;
using Glazer.Storage.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storage.Sqlite
{
    /// <summary>
    /// SQLite based <see cref="Block"/> storage.
    /// </summary>
    public class SqliteStorage : IStorage
    {
        private IKvScheme m_Scheme;

        private IKvTable m_Blocks;
        private IKvTable m_Properties;
        private IKvTable m_Forwards;
        
        private BlockId? m_InitialBlockId;
        private BlockId? m_LatestBlockId;

        /// <summary>
        /// Initialize a new <see cref="SqliteStorage"/> instance.
        /// </summary>
        /// <param name="DataDir"></param>
        public SqliteStorage(string DataDir, bool Truncate = false, bool RecreateSurface = false)
        {
            if (!Directory.Exists(DataDir))
                 Directory.CreateDirectory(DataDir);

            var BlockFile = Path.Combine(DataDir, "blocks.db");
            var CacheFile = Path.Combine(DataDir, "cache.db");

            if (Truncate && File.Exists(BlockFile))
                File.Delete(BlockFile);

            if (RecreateSurface && File.Exists(CacheFile))
                File.Delete(CacheFile);

            if (!File.Exists(BlockFile) && !SqliteKvScheme.MakeFile(BlockFile))
                throw new FileNotFoundException("No `blocks.db` file could be created.");

            if (!File.Exists(BlockFile) && !SqliteKvScheme.MakeFile(CacheFile))
                throw new FileNotFoundException("No `cache.db` file could be created.");

            m_Scheme = new SqliteKvScheme(BlockFile);
            m_Blocks = m_Scheme.Open("blocks") ?? m_Scheme.Create("blocks");
            m_Forwards = m_Scheme.Open("forwards") ?? m_Scheme.Create("forwards");
            m_Properties = m_Scheme.Open("properties") ?? m_Scheme.Create("properties");
        }

        /// <inheritdoc/>
        public BlockId InitialBlockId
        {
            get
            {
                lock (this)
                {
                    if (!m_InitialBlockId.HasValue)
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
                    if (!m_LatestBlockId.HasValue)
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

            while(!Token.IsCancellationRequested && Origin.IsValid)
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

            using var Writer = new PacketWriter();
            if (!Block.TryExport(Writer, Block))
                throw new InvalidOperationException("the block is not valid.");

            if (!await m_Blocks.SetAsync(Key, Writer.ToByteArray()) ||
                !await m_Forwards.SetGuidAsync(Key, NextId) ||
                !await m_Forwards.SetGuidAsync(Block.Previous.ToString(), BlockId.Guid))
                throw new InvalidOperationException("the block couldn't be stored.");

            lock (this)
            {
                if (InitialBlockId.Guid == Guid.Empty)
                    m_Properties.SetGuid("initial_block_id", (m_InitialBlockId = BlockId).Value.Guid);

                if (NextId == Guid.Empty) // Block.Previous.Id == LatestBlockId
                    m_Properties.SetGuid("latest_block_id", (m_LatestBlockId = BlockId).Value.Guid);
            }
        }

        /// <inheritdoc/>
        public async Task<BlockId> PutAsync(Block Block, CancellationToken Token = default)
        {
            while(true)
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

            m_Scheme.Dispose();
        }
    }
}
