using Glazer.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.IO
{
    /// <summary>
    /// Stream Read Delegate that is to refer `ReadAsync` method from stream..
    /// </summary>
    /// <param name="Buffer"></param>
    /// <param name="Offset"></param>
    /// <param name="Count"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    public delegate Task<int> FragmentReadDelegate(byte[] Buffer, int Offset, int Count, CancellationToken Token = default);

    public class StreamInputBuffer : IDisposable
    {
        private DisposingTokenSource m_Disposing;
        private FragmentReadDelegate m_Delegate;

        private byte[] m_Buffer;
        private int m_Offset, m_Length;
        private int m_Requires;

        private Task m_Task;

        /// <summary>
        /// Initialize a new <see cref="StreamInputBuffer"/> instance.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <param name="Capacity"></param>
        public StreamInputBuffer(FragmentReadDelegate Delegate, int Capacity = 4096)
        {
            m_Delegate = Delegate;
            m_Disposing = new DisposingTokenSource();

            m_Buffer = new byte[Capacity];
            m_Offset = m_Length = 0;
        }

        /// <summary>
        /// Get the task that fills the buffer.
        /// </summary>
        /// <returns></returns>
        private Task RequireBytes(int RequiredBytes)
        {
            async Task InternalAsync()
            {
                var Requeue = 0;
                while (!m_Disposing.IsCancellationRequested)
                {
                    var Requested = 0;

                    lock (this)
                    {
                        m_Requires += Requeue;

                        if ((Requested = m_Requires) <= 0 || Requested <= m_Length)
                        {
                            m_Task = Task.CompletedTask;
                            break;
                        }

                        m_Requires -= Requested;
                    }

                    if (m_Length > 0 && m_Offset > 0)
                    {
                        Buffer.BlockCopy(m_Buffer, m_Offset, m_Buffer, 0, m_Length);
                        m_Offset = 0;
                    }

                    if (m_Buffer.Length - m_Length <= 0)
                    {
                        continue;
                    }

                    var Length = await m_Delegate(m_Buffer, m_Length, m_Buffer.Length - m_Length, m_Disposing.Token);
                    if (Length <= 0)
                        throw new EndOfStreamException("No more bytes available.");

                    Requeue = Requested - Length;
                    m_Length += Length;
                }

                ThrowIfObjectDisposed();
            }

            lock(this)
            {
                if (m_Disposing.IsCancellationRequested)
                    return Task.CompletedTask;

                m_Requires += RequiredBytes;

                if (m_Task is null || m_Task.IsCompleted)
                    m_Task = InternalAsync();

                return m_Task;
            }
        }

        /// <summary>
        /// Read bytes with the buffering.
        /// </summary>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<byte[]> ReadBytes(int Length, CancellationToken Token = default)
        {
            ThrowIfObjectDisposed();

            var Tcs = new TaskCompletionSource();
            var Result = new byte[Length];

            using (Token.Register(Tcs.SetResult))
            {
                while (Length > 0)
                {
                    var Slice = Math.Min(m_Length, Length);
                    if (Slice <= 0)
                    {
                        var Filler = RequireBytes(Slice);
                        var Temp = await Task.WhenAny(Filler, Tcs.Task);

                        if (Temp == Tcs.Task)
                        {
                            Token.ThrowIfCancellationRequested();
                            break;
                        }

                        await Filler;
                        continue;
                    }

                    Buffer.BlockCopy(m_Buffer, m_Offset, Result, Result.Length - Length, Slice);
                    m_Offset += Slice; m_Length -= Slice; Length -= Slice;
                }
            }

            return Result;
        }

        /// <summary>
        /// Read <see cref="byte"/> asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<byte> ReadByte(CancellationToken Token = default)
        {
            ThrowIfObjectDisposed();

            var Tcs = new TaskCompletionSource();

            using (Token.Register(Tcs.SetResult))
            {
                while (true)
                {
                    if (m_Length <= 0)
                    {
                        var Filler = RequireBytes(1);
                        var Temp = await Task.WhenAny(Filler, Tcs.Task);

                        if (Temp == Tcs.Task)
                        {
                            Token.ThrowIfCancellationRequested();
                            break;
                        }

                        await Filler;
                        continue;
                    }

                    m_Offset++; m_Length--;
                    return m_Buffer[m_Offset - 1];
                }
            }

            throw new InvalidOperationException("Unreachable.");
        }

        /// <summary>
        /// Throw <see cref="ObjectDisposedException"/> if disposed.
        /// </summary>
        private void ThrowIfObjectDisposed()
        {
            if (m_Disposing.Token.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(StreamInputBuffer));
        }

        /// <summary>
        /// Dispose the <see cref="StreamInputBuffer"/> and wait completions of the internal async tasks.
        /// </summary>
        /// <returns></returns>
        public bool DisposeAndReturnState()
        {
            if (m_Disposing.DisposeAndReturnState())
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void Dispose() => m_Disposing.Dispose();
    }
}
