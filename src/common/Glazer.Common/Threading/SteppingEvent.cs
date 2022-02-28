using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Threading
{
    public class SteppingEvent
    {
        private TaskCompletionSource m_Steps, m_Blocks;
        private bool m_Completed = false;

        /// <summary>
        /// Waits for the current stepping is ended,
        /// And blocks the stepping.
        /// </summary>
        /// <returns></returns>
        public async ValueTask WaitAsync()
        {
            Task Steps;
            lock(this)
            {
                if ((m_Blocks is null || m_Blocks.Task.IsCompleted) && !m_Completed)
                    m_Blocks = new TaskCompletionSource();

                if (m_Steps is null)
                    Steps = Task.CompletedTask;

                else
                    Steps = m_Steps.Task;
            }

            await Steps.ConfigureAwait(false);
        }

        /// <summary>
        /// Reset the blocking that took by `<see cref="WaitAsync"/>` method.
        /// </summary>
        public ValueTask SetAsync()
        {
            lock(this)
            {
                if (m_Blocks is null)
                    return ValueTask.CompletedTask;

                m_Blocks.TrySetResult();
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Begins the current step asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async ValueTask<bool> BeginAsync(CancellationToken Token = default)
        {
            Task Blocks;
            lock(this)
            {
                if ((m_Steps is null || m_Steps.Task.IsCompleted) && !m_Completed)
                    m_Steps = new TaskCompletionSource();

                if (m_Blocks is null)
                    Blocks = Task.CompletedTask;

                else
                    Blocks = m_Blocks.Task;
            }

            var Tcs = new TaskCompletionSource();
            using (Token.Register(Tcs.SetResult))
            {
                await Task.WhenAny(Blocks, Tcs.Task);

                if (Tcs.Task.IsCompleted)
                {
                    await Tcs.Task;
                    return false;
                }

                await Blocks.ConfigureAwait(false);
                return true;
            }
        }

        /// <summary>
        /// Ends the current step asynchronously.
        /// </summary>
        /// <returns></returns>
        public ValueTask EndAsync()
        {
            lock(this)
            {
                if (m_Steps is null)
                    return ValueTask.CompletedTask;

                m_Steps.TrySetResult();
                return ValueTask.CompletedTask;
            }
        }

        /// <summary>
        /// Notify that the stepping is completed.
        /// </summary>
        /// <returns></returns>
        public bool Complete()
        {
            lock (this)
            {
                if (m_Completed)
                    return false;

                m_Completed = true;

                m_Steps?.TrySetResult();
                m_Blocks?.TrySetResult();
                return true;
            }

        }
    }
}
