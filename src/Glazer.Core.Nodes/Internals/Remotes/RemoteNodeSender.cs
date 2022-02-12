using Backrole.Core.Abstractions;
using Backrole.Core.Hosting;
using Backrole.Orp.Utilities;
using Glazer.Core.Helpers;
using Glazer.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Remotes
{
    using Redirectors = Dictionary<Guid, Func<object, bool>>;
    internal class RemoteNodeSender : IDisposable
    {
        private SemaphoreSlim m_Semaphore = new SemaphoreSlim(1);
        private Redirectors m_Redirectors = new();

        private MessageMapper m_Mapper;
        private Socket m_Socket;

        /// <summary>
        /// Initialize a new <see cref="RemoteNodeSender"/> instance.
        /// </summary>
        /// <param name="Tcp"></param>
        /// <param name="Mapper"></param>
        public RemoteNodeSender(TcpClient Tcp, MessageMapper Mapper)
        {
            m_Socket = Tcp.Client;
            m_Mapper = Mapper;
        }

        /// <summary>
        /// Send a request message to remote host.
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<object> Request(object Message, CancellationToken Token = default)
        {
            try { await m_Semaphore.WaitAsync(Token); }
            catch
            {
                Token.ThrowIfCancellationRequested();
                return null;
            }

            var Released = false;
            Guid NewId = SetRedirect(out var Redirect, out var TaskResult);
            try
            {
                using var Stream = new MemoryStream();
                using (var Writer = new EndianessWriter(Stream, null, true, true))
                {
                    Writer.Write(NewId);
                    Writer.Write((byte)0x01);
                    Writer.Write((byte)0x01);
                    m_Mapper.Encode(Writer, Message);
                }

                var Packet = Stream.ToArray();
                var LengthBytes = BitConverter.GetBytes(Packet.Length);

                if (!BitConverter.IsLittleEndian)
                     Array.Reverse(LengthBytes);

                if (await SendBytes(LengthBytes.Concat(Packet).ToArray()))
                {
                    using (Token.Register(() => Redirect(null)))
                    {
                        Released = true;
                        m_Semaphore.Release();

                        if ((Message = await TaskResult) is null)
                            Token.ThrowIfCancellationRequested();

                        return Message;
                    }
                }

                return null;
            }

            finally
            {
                Redirect(null);
                if (!Released)
                    m_Semaphore.Release();
            }
        }

        /// <summary>
        /// Set the redirect delegate for the receiver.
        /// </summary>
        /// <param name="Redirect"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        private Guid SetRedirect(out Func<object, bool> Redirect, out Task<object> Result)
        {
            var NewId = Guid.NewGuid();
            var Tcs = new TaskCompletionSource<object>();

            while (true)
            {
                lock (this)
                {
                    if (m_Redirectors.ContainsKey(NewId))
                    {
                        NewId = Guid.NewGuid();
                        continue;
                    }

                    m_Redirectors[NewId] = Redirect = Message =>
                    {
                        if (!Tcs.TrySetResult(Message))
                            return false;

                        lock(this)
                        {
                            m_Redirectors.Remove(NewId);
                            return true;
                        }
                    };

                    Result = Tcs.Task;
                    return NewId;
                }
            }
        }

        /// <summary>
        /// Dispatch a reply to the redirector - (`<see cref="Request(object, CancellationToken)"/>`).
        /// </summary>
        /// <param name="Guid"></param>
        /// <param name="Message"></param>
        public void DispatchReply(Guid Guid, object Message)
        {
            lock (this)
            {
                if (m_Redirectors.Remove(Guid, out var Redirect))
                    ThreadPool.QueueUserWorkItem(_ => Redirect(Message));
            }
        }

        /// <summary>
        /// Send reply to the remote host.
        /// </summary>
        /// <param name="Guid"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public async Task ReplyAsync(Guid Guid, object Message)
        {
            try { await m_Semaphore.WaitAsync(); }
            catch
            {
                return;
            }

            try
            {
                using var Stream = new MemoryStream();
                using (var Writer = new EndianessWriter(Stream, null, true, true))
                {
                    Writer.Write(Guid);
                    Writer.Write((byte)0x02);

                    if (Message is null)
                        Writer.Write((byte)0x00);

                    else
                    {
                        Writer.Write((byte)0x01);
                        m_Mapper.Encode(Writer, Message);
                    }

                }

                var Packet = Stream.ToArray();
                var LengthBytes = BitConverter.GetBytes(Packet.Length);

                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(LengthBytes);

                await SendBytes(LengthBytes.Concat(Packet).ToArray());
            }
            finally
            {
                m_Semaphore.Release();
            }
        }

        /// <summary>
        /// Send bytes to the remote host.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        private async Task<bool> SendBytes(byte[] Message)
        {
            var Buffer = new ArraySegment<byte>(Message);

            while (Buffer.Count > 0)
            {
                int Length;

                try { Length = await m_Socket.SendAsync(Buffer, SocketFlags.None); }
                catch
                {
                    if (m_Socket.Connected)
                        continue;

                    Length = 0;
                }

                if (Length <= 0)
                    return false;

                Buffer = new ArraySegment<byte>(Buffer.Array,
                    Buffer.Offset + Length, Buffer.Count - Length);
            }

            return true;
        }

        /// <inheritdoc/>
        public void Dispose() => m_Semaphore.Dispose();
    }
}
