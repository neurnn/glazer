using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Threading
{
    public class AutoResetEventAsync : IDisposable
    {
        private Queue<TaskCompletionSource> m_Tcs = new();
        private bool m_State = false, m_Disposed = false;

        /// <summary>
        /// Initialize a new <see cref="AutoResetEventAsync"/> instance.
        /// </summary>
        /// <param name="InitState"></param>
        public AutoResetEventAsync(bool InitState = false)
            => m_State = InitState;

        /// <summary>
        /// Wait for the event set.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> WaitAsync(CancellationToken Token = default)
        {
            TaskCompletionSource Tcs;
            while(true)
            {
                lock(this)
                {
                    if (m_Disposed)
                        return false;

                    if (m_State)
                    {
                        m_State = false;
                        return true;
                    }

                    if (Token.IsCancellationRequested)
                        return false;

                    m_Tcs.Enqueue(Tcs = new TaskCompletionSource());
                }

                using (Token.Register(() => Tcs.TrySetResult()))
                    await Tcs.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Try to wait event once.
        /// </summary>
        /// <returns></returns>
        public bool TryWait()
        {
            lock (this)
            {
                if (m_Disposed)
                    return false;

                if (m_State)
                {
                    m_State = false;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Signal event.
        /// </summary>
        public void Signal()
        {
            TaskCompletionSource Tcs;

            lock (this)
            {
                if (m_Disposed)
                    return;

                while (m_Tcs.TryDequeue(out Tcs))
                {
                    if (Tcs.Task.IsCompleted)
                        continue;

                    break;
                }

                m_State = true;
            }

            Tcs?.TrySetResult();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            while (true)
            {
                TaskCompletionSource Tcs;
                lock (this)
                {
                    m_Disposed = true;
                    if (!m_Tcs.TryDequeue(out Tcs))
                        break;
                }

                Tcs?.TrySetResult();
            }
        }
    }
}
