using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storage.Abstraction.Internals
{
    public class SurfaceKvTable : IKvTable
    {
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);
        private IStorage m_Storage;
        private IKvTable m_CacheSet;

        /// <summary>
        /// Initialize a new <see cref="SurfaceKvTable"/> that reads <see cref="IStorage"/>.
        /// </summary>
        /// <param name="Storage"></param>
        public SurfaceKvTable(IStorage Storage, IKvTable CacheSet)
        {
            m_Storage = Storage;
            m_CacheSet = CacheSet ?? new MemoryKvTable();
        }

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <inheritdoc/>
        public void Dispose()
        {
            m_CacheSet.Dispose();
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            var Latest = m_Storage.LatestBlockId;
            var Cursor = Latest;
            while (true)
            {
                var Value = await m_CacheSet.GetAsync(Key, Token);
                if (Value is not null)
                {
                    return Value;
                }

                var Block = await m_Storage.GetAsync(Cursor, Token);
                if (Block.Previous.IsValid)
                {
                    foreach(var Each in Block.Data)
                    {
                        if ((Value = await m_CacheSet.GetAsync(Each.Key)) is null)
                            await m_CacheSet.SetAsync(Each.Key, Each.Value);
                    }

                    Cursor = Block.Previous.Id;
                    continue;
                }

                return null;
            }
        }

        /// <inheritdoc/>
        public Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default) => TASK_FALSE;
    }
}
