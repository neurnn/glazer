using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.Core.Threading
{
    public class SubscriptionChannel<T>
    {
        private List<Func<T, Task>> m_Subscribers = new();
        private TaskCompletionSource m_Completion = new();
        private TaskCompletionSource m_Subscribed = new();

        /// <summary>
        /// Task that completed when the subject is completed.
        /// </summary>
        public Task Completion => m_Completion.Task;

        /// <summary>
        /// Try to complete the dispatcher.
        /// </summary>
        /// <returns></returns>
        public bool TryComplete() => m_Completion.TrySetResult();

        /// <summary>
        /// Subscribe the dispatcher.
        /// </summary>
        /// <param name="Subscriber"></param>
        /// <returns></returns>
        public IDisposable Subscribe(Func<T, Task> Subscriber)
        {
            lock (this)
            {
                m_Subscribers.Add(Subscriber);
                m_Subscribed.TrySetResult();
            }

            return new Disposable
            {
                Action = () =>
                {
                    lock (this)
                    {
                        m_Subscribers.Remove(Subscriber);
                        if (m_Subscribers.Count > 0)
                            return;

                        /* Reset the `subscribed` event propagation purpose tcs. */
                        if (m_Subscribed.Task.IsCompleted)
                            m_Subscribed = new();
                    }
                }
            };
        }

        /// <summary>
        /// Dispatch an item asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task DispatchAsync(T Item, CancellationToken Token = default)
        {
            var Tcs = new TaskCompletionSource();
            while (true)
            {
                await WaitForDispatchAsync(Tcs, Token);

                Queue<Func<T, Task>> Destinations;
                lock (this)
                {
                    if (m_Subscribers.Count <= 0)
                        continue;

                    Destinations = new Queue<Func<T, Task>>(m_Subscribers);
                }

                ThreadPool.QueueUserWorkItem(_ => _ = Dispatch(Item, Destinations));
                break;
            }
        }

        /// <summary>
        /// Dispatch an item to destination delegates.
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="Destinations"></param>
        /// <returns></returns>
        private static async Task Dispatch(T Item, Queue<Func<T, Task>> Destinations)
        {
            while (Destinations.TryDequeue(out var Subscriber))
                await Subscriber(Item);
        }

        /// <summary>
        /// Wait for <see cref="DispatchAsync(T, CancellationToken)"/> is ready to be called.
        /// </summary>
        /// <param name="Tcs"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task WaitForDispatchAsync(TaskCompletionSource Tcs, CancellationToken Token)
        {
            if (m_Completion.Task.IsCompleted)
                throw new InvalidOperationException("the dispatcher has been completed.");

            Token.ThrowIfCancellationRequested();
            using (Token.Register(Tcs.SetResult))
            {
                Task Subscription;

                lock (this)
                    Subscription = m_Subscribed.Task;

                /* Wait for the subscriber. */
                await Task.WhenAny(Subscription, Tcs.Task);
                Token.ThrowIfCancellationRequested();
            }
        }

        private struct Disposable : IDisposable
        {
            public Action Action;
            public void Dispose()
            {
                Action?.Invoke();
                Action = null;
            }
        }
    }
}
