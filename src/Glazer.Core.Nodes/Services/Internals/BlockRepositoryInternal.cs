using Backrole.Core.Abstractions;
using Glazer.Core.IO;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Services.Internals
{
    internal class BlockRepositoryInternal : IBlockRepository
    {
        private static readonly Task<Block> NOT_FOUND = Task.FromResult<Block>(null);

        private static readonly Task<HttpStatusCode> STATUS_OK = Task.FromResult(HttpStatusCode.OK);
        private static readonly Task<HttpStatusCode> STATUS_ERR = Task.FromResult(HttpStatusCode.InternalServerError);

        private (ulong Key, BlockDataSlots Slots)[] m_Partitions;

        private const int MGMT_SLOT_MAX_L32 = 0, MGMT_SLOT_MAX_H32 = 1;
        private BlockDataSlots m_Managements;

        /// <summary>
        /// Initialize a new <see cref="BlockRepositoryInternal"/> instance.
        /// </summary>
        /// <param name="Settings"></param>
        public BlockRepositoryInternal(IOptions<LocalNodeSettings> Settings)
        {
            var FullPath = Path.Combine(Settings.Value.DataDirectory.FullName, "blocks");
            if (!(DataDir = new DirectoryInfo(FullPath)).Exists)
            {
                Directory.CreateDirectory(DataDir.FullName);
                DataDir.Refresh();
            }

            if ((m_Managements = new BlockDataSlots(Path.Combine(DataDir.FullName, "info"))).IsCreatedNow)
            {
                m_Managements.Set(MGMT_SLOT_MAX_L32, BitConverter.GetBytes(uint.MinValue));
                m_Managements.Set(MGMT_SLOT_MAX_H32, BitConverter.GetBytes(uint.MinValue));
                LastBlockIndex = BlockIndex.Invalid;
            }

            else
            {
                var L32 = BitConverter.ToUInt32(m_Managements.Get(MGMT_SLOT_MAX_L32));
                var H32 = BitConverter.ToUInt32(m_Managements.Get(MGMT_SLOT_MAX_H32));
                LastBlockIndex = new BlockIndex(H32, L32);
            }

            m_Partitions = new (ulong Key, BlockDataSlots Slots)[Settings.Value.MaxLivePartitions];
        }

        /// <summary>
        /// Data Directory where block datas are stored.
        /// </summary>
        public DirectoryInfo DataDir { get; }

        /// <inheritdoc/>
        public BlockIndex LastBlockIndex { get; private set; }

        /// <summary>
        /// Get <see cref="BlockDataSlots"/> instance.
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="L"></param>
        /// <returns></returns>
        private BlockDataSlots GetPartition(BlockIndex Index, out uint L)
        {
            BlockIndex.MakePartitionNumbers(Index, out var S, out var P, out L);
            var Key = (ulong)S << 32 | P;
            int Empty = -1, Early = -1;
            var EarlyAccess = DateTime.Now;

            for (var i = 0; i < m_Partitions.Length; ++i)
            {
                if (m_Partitions[i].Slots is null)
                {
                    Empty = i;
                    continue;
                }

                if (m_Partitions[i].Slots.LastAccessTime < EarlyAccess)
                {
                    EarlyAccess = m_Partitions[i].Slots.LastAccessTime;
                    Early = i;
                }

                if (m_Partitions[i].Key == Key)
                    return m_Partitions[i].Slots;
            }

            var Name = Path.Combine(DataDir.FullName, $"slots.{Key.ToString("x14")}");
            if (Empty >= 0)
            {
                m_Partitions[Empty] = (Key, new BlockDataSlots(Name));
                return m_Partitions[Empty].Slots;
            }

            m_Partitions[Early].Slots.Dispose();
            m_Partitions[Early] = (Key, new BlockDataSlots(Name));
            return m_Partitions[Early].Slots;
        }

        /// <inheritdoc/>
        public Task<Block> GetAsync(BlockIndex BlockIndex, CancellationToken Token)
        {
            if (LastBlockIndex < BlockIndex)
                return NOT_FOUND;

            return Task.FromResult(Unpack(GetPartition(BlockIndex, out var L).Get(L)));
        }

        /// <summary>
        /// Unpack the block.
        /// </summary>
        /// <param name="Blob"></param>
        /// <returns></returns>
        private static Block Unpack(byte[] Blob)
        {
            if (Blob is null)
                return  null;

            using var Stream = new MemoryStream(Blob, true);
            using (var Reader = new EndianessReader(Stream, null, true, true))
            {
                try { return Reader.ReadBlock(); }
                catch { }

                return null;
            }
        }

        /// <inheritdoc/>
        public Task<HttpStatusCode> PutAsync(Block Block, CancellationToken Token)
        {
            var Index = Block.Header.Index;
            if (GetPartition(Index, out var L).Set(L, Pack(Block)))
            {
                var Max = Index > LastBlockIndex ? Index : LastBlockIndex;
                if (Max != LastBlockIndex)
                {
                    m_Managements.Set(MGMT_SLOT_MAX_L32, BitConverter.GetBytes(Max.L32));
                    m_Managements.Set(MGMT_SLOT_MAX_H32, BitConverter.GetBytes(Max.H32));
                    LastBlockIndex = Max;
                }

                return STATUS_OK;
            }

            return STATUS_ERR;
        }

        /// <summary>
        /// Pack to block.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        private static byte[] Pack(Block Block)
        {
            using var Stream = new MemoryStream();
            using (var Writer = new EndianessWriter(Stream, null, true, true))
                Writer.WriteWithoutValidation(Block);

            return Stream.ToArray();
        }
    }
}
