using Glazer.Kvdb.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Extensions.Internals
{
    /// <summary>
    /// Separate the <see cref="IKvTable"/>'s Read and Write.
    /// </summary>
    internal class Duplexer : IKvTable
    {
        private Dictionary<string, byte[]> m_Cache = new();
        private IKvTable m_Read, m_Write;

        /// <summary>
        /// Initialize a new <see cref="Duplexer"/> instance.
        /// </summary>
        /// <param name="Read"></param>
        /// <param name="Write"></param>
        public Duplexer(IKvTable Read, IKvTable Write)
        {
            if (Write.IsReadOnly)
                throw new InvalidOperationException("No `Write` table is writable.");

            m_Read = Read;
            m_Write = Write;
        }

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return null;

            lock (m_Cache)
            {
                if (m_Cache.TryGetValue(Key, out var Value))
                    return Value;
            }

            return await m_Read.GetAsync(Key, Token);
        }

        /// <inheritdoc/>
        public async Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return false;

            if (await m_Write.SetAsync(Key, Value, Token))
            {
                lock (m_Cache)
                    m_Cache[Key] = Value;

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_Read.Dispose();
            m_Write.Dispose();
        }
    }
}
