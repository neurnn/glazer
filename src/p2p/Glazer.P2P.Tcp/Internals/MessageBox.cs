using Backrole.Crypto;
using Glazer.Common.Common;
using Glazer.P2P.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.P2P.Tcp.Internals
{
    /// <summary>
    /// Holds the message before the expiration reached.
    /// </summary>
    internal class MessageBox : List<Message>, IAsyncDisposable, IDisposable
    {
        private CancellationTokenSource m_Cts = new();
        private Task m_Timer;

        /// <summary>
        /// Initialize a new <see cref="MessageBox"/> instance.
        /// </summary>
        public MessageBox() => m_Timer = RunLoop(m_Cts.Token);

        /// <summary>
        /// Check the message is still on the message box or not.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public bool Check(Message Message)
        {
            lock (this)
            {
                return FindIndex(X => X.Sender == Message.Sender) >= 0;
            }
        }

        /// <summary>
        /// Push the message to the expiration timer.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public MessageBox Push(Message Message)
        {
            if (Message.Sender.IsValid)
            {
                lock(this)
                {
                    if (!Check(Message))
                         Add(Message);

                    return this;
                }
            }

            return this;
        }

        /// <summary>
        /// Run the expiration timer.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task RunLoop(CancellationToken Token)
        {
            var Queue = new Queue<Message>();
            while (!Token.IsCancellationRequested)
            {
                var Origin = TimeStamp.Now;
                var Minimal = null as TimeStamp?;

                lock (this)
                {
                    foreach (var Each in this)
                    {
                        if (Each.Expiration > Origin)
                        {
                            if (!Minimal.HasValue)
                                Minimal = Each.Expiration;

                            else if (Minimal.Value > Each.Expiration)
                                Minimal = Each.Expiration;

                            continue;
                        }

                        Queue.Enqueue(Each);
                    }

                    while (Queue.TryDequeue(out var Each))
                        Remove(Each);
                }


                if (Minimal.HasValue)
                {
                    var Remains = (int) Math.Min(Math.Max((Minimal.Value.Value - Origin.Value) * 1000, 0), 5000);
                    if (Remains > 0)
                    {
                        try { await Task.Delay(Remains, Token); }
                        catch
                        {
                        }
                    }

                    continue;
                }

                try { await Task.Delay(200, Token); }
                catch
                {
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            CancellationTokenSource Cts;
            lock (this)
            {
                if ((Cts = m_Cts) is null)
                    return;

                m_Cts = null;
            }

            using (Cts)
            {
                Cts.Cancel();
                await m_Timer;

                Clear();
            }
        }
    }
}
