using Glazer.Kvdb.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Extensions.Internals
{
    /// <summary>
    /// Overlay the <see cref="IKvTable"/>'s Read.
    /// </summary>
    internal class Overlay : IKvTable
    {
        private HashSet<string> m_Masks = new();
        private IKvTable m_Table, m_Overlay;

        /// <summary>
        /// Initialize a new <see cref="Overlay"/> instance.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Overlay"></param>
        public Overlay(IKvTable Table, IKvTable Overlay)
        {
            m_Table = Table;
            m_Overlay = Overlay;
        }

        /// <summary>
        /// Test whether the key is masked or not.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private bool IsMasked(string Key)
        {
            lock (m_Masks)
                return m_Masks.Contains(Key);
        }

        /// <inheritdoc/>
        public bool IsReadOnly => m_Table.IsReadOnly;

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return null;

            if (IsMasked(Key))
                return await m_Table.GetAsync(Key, Token);

            var Value = await m_Overlay.GetAsync(Key, Token);
            if (Value is null && !Token.IsCancellationRequested)
            {
                return await m_Table.GetAsync(Key, Token);
            }

            return Value;
        }

        /// <inheritdoc/>
        public async Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return false;

            if (await m_Table.SetAsync(Key, Value, Token))
            {
                lock (m_Masks)
                    m_Masks.Add(Key);

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_Table.Dispose();
            m_Overlay.Dispose();
        }
    }
}
