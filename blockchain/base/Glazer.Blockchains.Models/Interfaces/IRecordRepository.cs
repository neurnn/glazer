using Glazer.Core.Cryptography;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    /// <summary>
    /// Saves the record of transaction or Loads saved records.
    /// </summary>
    public interface IRecordRepository
    {
        /// <summary>
        /// Code ID that this record repository is related with.
        /// </summary>
        Guid CodeId { get; }

        /// <summary>
        /// Create a view that scopes the specific Key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        IRecordView CreateView(string Key);

        /// <summary>
        /// Create a tracker that tracks the record's history.
        /// </summary>
        /// <param name="Key">Null to tracking all keys</param>
        /// <param name="Origin">Origin of the tracker</param>
        /// <returns></returns>
        IRecordTracker CreateTracker(string Key = null, TransactionRef Origin = default);

        /// <summary>
        /// Initialize a record for the Key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Record> NewAsync(string Key, CancellationToken Token = default);

        /// <summary>
        /// Get the record for the Key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Record> GetAsync(string Key, CancellationToken Token = default);

        /// <summary>
        /// Set the record with previous ETag against conflict state.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        Task<bool> SetAsync(string Key, Record Data, CancellationToken Token = default);
    }
}
