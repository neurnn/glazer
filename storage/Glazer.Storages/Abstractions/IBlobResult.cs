using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages.Abstractions
{
    /// <summary>
    /// Blob Result interface.
    /// </summary>
    public interface IBlobResult : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Status Code of the blob storage operation.
        /// </summary>
        BlobStatus Status { get; }

        /// <summary>
        /// Creation Time of the blob.
        /// </summary>
        DateTime CreationTime { get; }

        /// <summary>
        /// Modified Time of the blob.
        /// </summary>
        DateTime ModifiedTime { get; }

        /// <summary>
        /// Etag that received.
        /// </summary>
        string ETag { get; }

        /// <summary>
        /// Read the blob asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<BlobData> ReadAsync(CancellationToken Token = default);
    }
}
