using Glazer.Storages.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storages.Server.Internals
{
    internal class BlobLockManager : IAsyncDisposable
    {
        private Dictionary<Guid, IBlobLockResult> m_Lock = new();

        /// <summary>
        /// Update the expirations.
        /// </summary>
        private void Update()
        {
            IBlobLockResult[] Locks;
            lock (this)
            {
                var Keys = m_Lock
                    .Where(X =>
                    {
                        var LTime = X.Value.LockedTime;
                        if (LTime.Kind != DateTimeKind.Utc)
                            LTime = LTime.ToUniversalTime();

                        LTime = LTime.AddSeconds(X.Value.Expiration.TotalSeconds);
                        return LTime.Ticks < DateTime.UtcNow.Ticks;
                    })
                    .Select(X => X.Key)
                    .ToArray();

                if (Keys.Length <= 0)
                    return;

                Locks = Keys.Select(X => m_Lock[X]).ToArray();
                foreach (var Each in Keys)
                    m_Lock.Remove(Each);
            }

            foreach (var Each in Locks)
            {
                try { Each.Dispose(); }
                catch { }
            }
        }

        /// <summary>
        /// Register the lock information.
        /// </summary>
        /// <param name="Lock"></param>
        /// <returns></returns>
        public Guid Register(IBlobLockResult Lock)
        {
            try
            {
                while (true)
                {
                    Guid New = Guid.NewGuid();
                    lock (this)
                    {
                        if (m_Lock.TryGetValue(New, out _))
                            continue;

                        m_Lock[New] = Lock;
                        return New;
                    }
                }
            }

            finally { Update(); }
        }

        /// <summary>
        /// Unregister the lock information and returns itself.
        /// </summary>
        /// <param name="Guid"></param>
        /// <param name="Lock"></param>
        /// <returns></returns>
        public bool Unregister(Guid Guid, out IBlobLockResult Lock)
        {
            try
            {
                lock (this)
                {
                    return m_Lock.Remove(Guid, out Lock);
                }
            }
            finally { Update(); }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            while(true)
            {
                IBlobLockResult Lock;

                lock(this)
                {
                    if (m_Lock.Keys.Count <= 0)
                        break;

                    if (!m_Lock.Remove(m_Lock.Keys.First(), out Lock))
                        continue;
                }

                try { await Lock.DisposeAsync(); }
                catch { }
            }
        }
    }
}
