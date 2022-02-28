using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Threading
{
    /// <summary>
    /// Variation of <see cref="CancellationTokenSource"/>.
    /// </summary>
    public class DisposingTokenSource : IDisposable
    {
        private static readonly CancellationToken TOKEN = new CancellationToken(true);
        private CancellationTokenSource m_TokenSource;

        /// <summary>
        /// Initialize a new <see cref="DisposingTokenSource"/> instance.
        /// </summary>
        public DisposingTokenSource() => m_TokenSource = new();

        /// <summary>
        /// Initialize a new <see cref="DisposingTokenSource"/> that triggered after the delay.
        /// </summary>
        /// <param name="Delay"></param>
        public DisposingTokenSource(TimeSpan Delay) => m_TokenSource = new(Delay);

        /// <summary>
        /// Initialize a new <see cref="DisposingTokenSource"/> using externally created <see cref="CancellationTokenSource"/>.
        /// </summary>
        /// <param name="TokenSource"></param>
        private DisposingTokenSource(CancellationTokenSource TokenSource) => m_TokenSource = TokenSource;

        /// <summary>
        /// Creates a System.Threading.CancellationTokenSource that will be in the canceled
        /// state when any of the source tokens in the specified array are in the canceled
        /// state.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static DisposingTokenSource CreateLinked(CancellationToken Token)
            => new DisposingTokenSource(CancellationTokenSource.CreateLinkedTokenSource(Token));

        /// <summary>
        /// Creates a System.Threading.CancellationTokenSource that will be in the canceled
        /// state when any of the source tokens in the specified array are in the canceled
        /// state.
        /// </summary>
        /// <param name="Token1"></param>
        /// <param name="Token2"></param>
        /// <returns></returns>
        public static DisposingTokenSource CreateLinked(CancellationToken Token1, CancellationToken Token2)
            => new DisposingTokenSource(CancellationTokenSource.CreateLinkedTokenSource(Token1, Token2));

        /// <summary>
        /// Creates a System.Threading.CancellationTokenSource that will be in the canceled
        /// state when any of the source tokens in the specified array are in the canceled
        /// state.
        /// </summary>
        /// <param name="Tokens"></param>
        /// <returns></returns>
        public static DisposingTokenSource CreateLinked(params CancellationToken[] Tokens)
            => new DisposingTokenSource(CancellationTokenSource.CreateLinkedTokenSource(Tokens));

        /// <summary>
        /// Gets whether cancellation has been requested for this <see cref="DisposingTokenSource"/>.
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                lock(this)
                {
                    if (m_TokenSource is not null)
                        return m_TokenSource.IsCancellationRequested;

                    return true;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="CancellationToken"/> associated with this <see cref="DisposingTokenSource"/>.
        /// </summary>
        public CancellationToken Token
        {
            get
            {
                lock(this)
                {
                    if (m_TokenSource is not null)
                        return m_TokenSource.Token;

                    return TOKEN;
                }
            }
        }

        /// <summary>
        /// Communicates a request for cancellation.
        /// </summary>
        public void Cancel()
        {
            CancellationTokenSource Cts;

            lock (this)
            {
                if ((Cts = m_TokenSource) is null)
                    return;
            }

            Cts.Cancel();
        }

        /// <inheritdoc/>
        public void Dispose() => DisposeAndReturnState();

        /// <summary>
        /// Dispose this <see cref="DisposingTokenSource"/> and return its disposed now or not.
        /// </summary>
        /// <returns></returns>
        public bool DisposeAndReturnState()
        {
            CancellationTokenSource Cts;

            lock (this)
            {
                if ((Cts = m_TokenSource) is null)
                    return false;

                m_TokenSource = null;
            }

            Cts.Cancel();
            Cts.Dispose();
            return true;
        }
    }
}
