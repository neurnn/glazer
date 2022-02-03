using System;

namespace Glazer.Storages.Abstractions
{
    /// <summary>
    /// Blob Lock Result interface.
    /// </summary>
    public interface IBlobLockResult : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Status Code of the locking operation.
        /// </summary>
        BlobStatus Status { get; }

        /// <summary>
        /// Locked Time.
        /// </summary>
        DateTime LockedTime { get; }

        /// <summary>
        /// Expiration (from LockedTime).
        /// </summary>
        TimeSpan Expiration { get; }
    }
}
