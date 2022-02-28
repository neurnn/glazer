using Glazer.Kvdb.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Memory
{
    public class MemoryKvTableView : IKvTable
    {
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);
        private MemoryKvTable m_Table;

        /// <summary>
        /// Initialize a new <see cref="MemoryKvTableView"/> using the <see cref="MemoryKvTable"/> instance.
        /// </summary>
        /// <param name="Table"></param>
        public MemoryKvTableView(MemoryKvTable Table) => m_Table = Table;

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string Key, CancellationToken Token = default) => m_Table.GetAsync(Key, Token);

        /// <inheritdoc/>
        public Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default) => TASK_FALSE;

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
