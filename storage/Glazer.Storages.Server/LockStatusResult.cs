using Glazer.Storages.Abstractions;
using System;
using System.Threading.Tasks;

namespace Glazer.Storages.Server
{
    internal struct LockStatusResult : IBlobLockResult
    {
        /// <inheritdoc/>
        public BlobStatus Status { get; set; }

        /// <inheritdoc/>
        public DateTime LockedTime => DateTime.MinValue;

        /// <inheritdoc/>
        public TimeSpan Expiration => TimeSpan.Zero;

        /// <inheritdoc/>
        public void Dispose() { }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    }
    
}
