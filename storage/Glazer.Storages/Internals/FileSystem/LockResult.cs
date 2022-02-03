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
    internal class LockResult : IBlobLockResult
    {
        private TaskCompletionSource m_Tcs;
        private Task m_Timer;

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
        /// <param name="Stream"></param>
        /// <param name="Expiration"></param>
        public LockResult(FileStream Stream, TimeSpan Expiration)
        {
            Status = BlobStatus.Ok;
            LockedTime = DateTime.UtcNow;
            this.Expiration = Expiration;

            m_Tcs = new TaskCompletionSource();
            m_Timer = RunTimer(Stream);
        }

        /// <summary>
        /// Run the expiration timer.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task RunTimer(FileStream Stream)
        {
            using var Cts = new CancellationTokenSource(Expiration);
            using (Cts.Token.Register(() => m_Tcs.TrySetResult()))
                await m_Tcs.Task;

            await Stream.DisposeAsync();
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
            if (m_Tcs != null)
            {
                m_Tcs?.TrySetResult();
                await m_Timer;
            }
        }
    }
}
