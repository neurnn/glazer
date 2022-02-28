using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Abstractions
{
    /// <summary>
    /// A table that contains `Key-Value` sets.
    /// </summary>
    public interface IKvTable : IDisposable
    {
        /// <summary>
        /// Indicates whether the table is read-only or not.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Get the value by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        Task<byte[]> GetAsync(string Key, CancellationToken Token = default);

        /// <summary>
        /// Set the value by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default);
    }
}
