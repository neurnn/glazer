using Glazer.Kvdb.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Extensions.Internals
{
    internal class Prefix : IKvTable
    {
        private static readonly Task<byte[]> TASK_NULL = Task.FromResult(null as byte[]);
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);

        private IKvTable m_Table;
        private string m_Prefix;

        /// <summary>
        /// Initialize a new <see cref="Prefix"/> instance.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Prefix"></param>
        public Prefix(IKvTable Table, string Prefix)
        {
            if (string.IsNullOrWhiteSpace(Prefix))
                throw new InvalidOperationException("Prefix shouldn't be null or white-space.");

            m_Table = Table;
            m_Prefix = Prefix;
        }

        /// <inheritdoc/>
        public bool IsReadOnly => m_Table.IsReadOnly;

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return TASK_NULL;

            return m_Table.GetAsync($"{m_Prefix}{Key}", Token);
        }

        /// <inheritdoc/>
        public Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return TASK_FALSE;

            return m_Table.SetAsync($"{m_Prefix}{Key}", Value, Token);
        }

        /// <inheritdoc/>
        public void Dispose() => m_Table.Dispose();
    }
}
