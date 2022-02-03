using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages.Abstractions
{
    /// <summary>
    /// Blob Storage interface.
    /// </summary>
    public interface IBlobStorage : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Indicates whether the storage is local or not.
        /// </summary>
        bool IsLocalStorage { get; }

        /// <summary>
        /// Test whether the storage connection is alive or not.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<BlobStatus> TestAsync(CancellationToken Token = default);

        /// <summary>
        /// Lock a key with expiration. (use this carefully)
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Expiration"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IBlobLockResult> LockAsync(string Key, TimeSpan? Expiration = null, CancellationToken Token = default);

        /// <summary>
        /// Gets a blob by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IBlobResult> GetAsync(string Key, CancellationToken Token = default);

        /// <summary>
        /// Gets a blob by its key.
        /// This reads a blob only if the e-tag matched.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Etag"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IBlobResult> GetAsync(string Key, string Etag, CancellationToken Token = default);

        /// <summary>
        /// Posts a blob by its key.
        /// This creates a blob only if the key does not exist.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IBlobResult> PostAsync(string Key, BlobData Data, CancellationToken Token = default);

        /// <summary>
        /// Put a blob by its key.
        /// This updates a blob if the key exists (and ETag matched if specified).
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IBlobResult> PutAsync(string Key, BlobData Data, CancellationToken Token = default);

        /// <summary>
        /// Delete a blob by its key.
        /// This updates a blob if the key exists (and ETag matched if specified).
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<BlobStatus> DeleteAsync(string Key, string Etag = null, CancellationToken Token = default);
    }
}
