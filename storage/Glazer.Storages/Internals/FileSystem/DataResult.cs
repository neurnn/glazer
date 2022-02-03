using Glazer.Storages.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages.Internals.FileSystem
{
    internal class DataResult : IBlobResult
    {
        private BlobData? m_Data;

        /// <summary>
        /// Initialize a new <see cref="DataResult"/> instance.
        /// </summary>
        /// <param name="Status"></param>
        public DataResult(BlobStatus Status)
        {
            this.Status = Status;
            CreationTime = ModifiedTime = DateTime.MinValue;
        }

        /// <summary>
        /// Initialize a new <see cref="DataResult"/> instance.
        /// </summary>
        /// <param name="Data"></param>
        public DataResult(BlobData Data, DateTime CreationTime, DateTime ModifiedTime)
        {
            m_Data = Data;

            Status = BlobStatus.Ok;
            ETag = Data.Etag;

            this.CreationTime = CreationTime;
            this.ModifiedTime = ModifiedTime;
        }

        /// <inheritdoc/>
        public BlobStatus Status { get; }

        /// <inheritdoc/>
        public DateTime CreationTime { get; }

        /// <inheritdoc/>
        public DateTime ModifiedTime { get; }

        /// <inheritdoc/>
        public string ETag { get; }

        /// <summary>
        /// Read the blob data asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<BlobData> ReadAsync(CancellationToken Token = default)
        {
            if (m_Data.HasValue)
            {
                return new BlobData
                {
                    Etag = m_Data.Value.Etag,
                    Data = m_Data.Value.Data
                };
            }

            throw new InvalidOperationException($"No data available.");
        }

        /// <inheritdoc/>
        public void Dispose() { }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    }
}
