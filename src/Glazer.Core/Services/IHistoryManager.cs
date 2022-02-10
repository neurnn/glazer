using Backrole.Crypto;
using Glazer.Core.Models.Blocks;
using Glazer.Core.Models.Histories;
using Glazer.Core.Records;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Services
{
    /// <summary>
    /// History Manager instance.
    /// </summary>
    public interface IHistoryManager
    {
        /// <summary>
        /// Indicates whether the history manager is read-only or not.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Write a history value to the history manager asynchronously.
        /// This will be always failed if <see cref="IsReadOnly"/> value is true.
        /// </summary>
        /// <param name="BlockIndex"></param>
        /// <param name="TransactionId"></param>
        /// <param name="Key"></param>
        /// <param name="Blob"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> PutAsync(HistoryColumnKey Key, BlockIndex BlockIndex, HashValue TransactionId, byte[] Blob, CancellationToken Token = default);

        /// <summary>
        /// Gets a <see cref="HistoryColumn"/> using its column key asynchronously.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<HistoryColumn> GetAsync(HistoryColumnKey Key, CancellationToken Token = default);

        /// <summary>
        /// Gets a <see cref="HistoryColumn"/>s using its row key asynchronously.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<HistoryColumn[]> GetAsync(HistoryKey Key, CancellationToken Token = default);
    }
}
