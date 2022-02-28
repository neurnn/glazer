using Glazer.Kvdb.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Extensions.Internals
{
    internal class Postfix : IKvTable
    {
        private static readonly Task<byte[]> TASK_NULL = Task.FromResult(null as byte[]);
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);

        private IKvTable m_Table;
        private string m_Postfix;

        /// <summary>
        /// Initialize a new <see cref="Postfix"/> instance.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Postfix"></param>
        public Postfix(IKvTable Table, string Postfix)
        {
            if (string.IsNullOrWhiteSpace(Postfix))
                throw new InvalidOperationException("Prefix shouldn't be null or white-space.");

            m_Table = Table;
            m_Postfix = Postfix;
        }

        /// <inheritdoc/>
        public bool IsReadOnly => m_Table.IsReadOnly;

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return TASK_NULL;

            return m_Table.GetAsync($"{Key}{m_Postfix}", Token);
        }

        /// <inheritdoc/>
        public Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return TASK_FALSE;

            return m_Table.SetAsync($"{Key}{m_Postfix}", Value, Token);
        }

        /// <inheritdoc/>
        public void Dispose() => m_Table.Dispose();
    }
}
