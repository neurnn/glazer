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
    /// Disable the calling to <see cref="IDisposable.Dispose"/> method of the table.
    /// </summary>
    internal class DisableDispose : IKvTable
    {
        private IKvTable m_Table;

        /// <summary>
        /// Initialize a new <see cref="DisableDispose"/> instance.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Prefix"></param>
        public DisableDispose(IKvTable Table)
        {
            m_Table = Table;
        }

        /// <inheritdoc/>
        public bool IsReadOnly => m_Table.IsReadOnly;

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            return m_Table.GetAsync($"{Key}", Token);
        }

        /// <inheritdoc/>
        public Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default)
        {
            return m_Table.SetAsync($"{Key}", Value, Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
