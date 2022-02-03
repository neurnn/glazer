using Glazer.Storages.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages.Server
{
    internal struct StatusResult : IBlobResult
    {
        /// <inheritdoc/>
        public BlobStatus Status { get; set; }

        /// <inheritdoc/>
        public DateTime CreationTime => DateTime.MinValue;

        /// <inheritdoc/>
        public DateTime ModifiedTime => DateTime.MinValue;

        /// <inheritdoc/>
        public string ETag { get; set; }

        /// <inheritdoc/>
        public void Dispose() { }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <inheritdoc/>
        public Task<BlobData> ReadAsync(CancellationToken Token = default)
        {
            throw new NotSupportedException();
        }
    }
    
}
