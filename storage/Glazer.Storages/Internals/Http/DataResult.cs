using Glazer.Storages.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages.Internals.Http
{
    internal class DataResult : IBlobResult
    {
        private HttpBlobStorage m_Storage;
        private HttpResponseMessage m_Http;
        private string m_Key;

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
        /// <param name="Http"></param>
        public DataResult(HttpResponseMessage Http)
        {
            m_Http = Http;
            Status = BlobStatus.Ok;

            ExtractHeaders(out var CT, out var MT, out var ETag);
            CreationTime = CT; ModifiedTime = MT; this.ETag = ETag;
        }

        /// <summary>
        /// Initialize a new <see cref="DataResult"/> instance.
        /// </summary>
        /// <param name="Http"></param>
        /// <param name="Storage"></param>
        /// <param name="Key"></param>
        public DataResult(HttpResponseMessage Http, HttpBlobStorage Storage, string Key)
        {
            m_Http = Http;
            m_Storage = Storage;
            m_Key = Key;

            ExtractHeaders(out var CT, out var MT, out var ETag);
            CreationTime = CT; ModifiedTime = MT; this.ETag = ETag;
        }

        /// <summary>
        /// Extract the time headers.
        /// </summary>
        /// <param name="CT"></param>
        /// <param name="MT"></param>
        private void ExtractHeaders(out DateTime CT, out DateTime MT, out string Etag)
        {
            var Creation = m_Http.Headers
               .Where(X => X.Key.Equals("X-Glazer-BlobCTime", StringComparison.OrdinalIgnoreCase))
               .SelectMany(X => X.Value).FirstOrDefault();

            var Modified = m_Http.Headers
               .Where(X => X.Key.Equals("X-Glazer-BlobMTime", StringComparison.OrdinalIgnoreCase))
               .SelectMany(X => X.Value).FirstOrDefault();

            CT = MT = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(Creation) && double.TryParse(Creation, out var CTSecs))
                CT = DateTime.UnixEpoch.AddSeconds(CTSecs);

            if (!string.IsNullOrWhiteSpace(Modified) && double.TryParse(Modified, out var MTSecs))
                MT = DateTime.UnixEpoch.AddSeconds(MTSecs);

            Etag = m_Http.Headers
               .Where(X => X.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
               .SelectMany(X => X.Value).FirstOrDefault();

            if (Etag != null)
                Etag = Etag.Trim(' ', '\t', '\'', '"');

            if (Etag != null && Etag.Length <= 0)
                Etag = null;
        }

        /// <inheritdoc/>
        public BlobStatus Status { get; }

        /// <inheritdoc/>
        public DateTime CreationTime { get; }

        /// <inheritdoc/>
        public DateTime ModifiedTime { get; }

        /// <inheritdoc/>
        public string ETag { get; }

        /// <inheritdoc/>
        public async Task<BlobData> ReadAsync(CancellationToken Token = default)
        {
            if (Status == BlobStatus.Ok && m_Http != null)
            {
                try
                {
                    var Etag = m_Http.Headers
                       .Where(X => X.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase))
                       .SelectMany(X => X.Value).FirstOrDefault();

                    if (Etag != null)
                        Etag = Etag.Trim(' ', '\t', '\'', '"');

                    if (m_Storage != null)
                    {
                        using var Temp = await m_Storage.GetAsync(m_Key, Etag, Token);
                        return await Temp.ReadAsync(Token);
                    }

                    var Data = await m_Http.Content.ReadAsByteArrayAsync(Token);
                    return new BlobData
                    {
                        Etag = string.IsNullOrWhiteSpace(Etag) ? null : Etag,
                        Data = Data
                    };
                }
                catch { }
            }

            return default;
        }

        /// <inheritdoc/>
        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            HttpResponseMessage Http;
            lock(this)
            {
                if ((Http = m_Http) is null)
                    return ValueTask.CompletedTask;

                m_Http = null;
            }

            Http.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
