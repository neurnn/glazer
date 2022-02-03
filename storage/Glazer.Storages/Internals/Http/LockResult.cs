using Glazer.Storages.Abstractions;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Glazer.Storages.Internals.Http
{
    internal class LockResult : IBlobLockResult
    {
        private HttpClient m_Http;
        private UnlockRequest m_Request;

        /// <summary>
        /// Initialize a new <see cref="LockResult"/> instance.
        /// </summary>
        /// <param name="Status"></param>
        public LockResult(BlobStatus Status)
        {
            this.Status = Status;
            LockedTime = DateTime.MinValue;
            Expiration = TimeSpan.Zero;
        }

        /// <summary>
        /// Initialize a new <see cref="LockResult"/> instance.
        /// </summary>
        /// <param name="Http"></param>
        /// <param name="Response"></param>
        public LockResult(HttpClient Http, LockResponse Response, string Key)
        {
            m_Http = Http;
            m_Request = new UnlockRequest
            {
                Key = Key,
                Token = Response.Token
            };

            Status = BlobStatus.Ok;            
            LockedTime = DateTime.UnixEpoch.AddSeconds(Response.LockedTime);
            Expiration = TimeSpan.FromSeconds(Response.Expiration);
        }

        /// <inheritdoc/>
        public BlobStatus Status { get; }

        /// <inheritdoc/>
        public DateTime LockedTime { get; }

        /// <inheritdoc/>
        public TimeSpan Expiration { get; }

        /// <inheritdoc/>
        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            HttpClient Http;
            lock (this)
            {
                if ((Http = m_Http) is null)
                    return;

                m_Http = null;
            }

            try
            {
                using var _ = await Http
                    .PostAsync("unlock", UnlockRequest.Make(m_Request));
            }
            catch
            {
            }
        }
    }
}
