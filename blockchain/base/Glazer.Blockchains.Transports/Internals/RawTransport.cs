using Glazer.Blockchains.Models;
using Glazer.Blockchains.Models.Interfaces;
using Glazer.Blockchains.Models.Internals;
using Glazer.Blockchains.Packets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Transports.Internals
{
    internal class RawTransport
    {
        private Channel<JObject> m_Channel;
        private TcpClient m_Tcp;

        private static readonly Assembly ASSEMBLY = typeof(Notify_NodeInformation).Assembly;
        private static readonly Dictionary<string, Type> TYPE_TABLE = new();
        static RawTransport()
        {
            foreach(var Each in ASSEMBLY.GetTypes())
                TYPE_TABLE[Each.Name] = Each;
        }

        private TaskCompletionSource m_Tcs;

        /// <summary>
        /// Initialize a new <see cref="TcpClient"/> instance.
        /// </summary>
        /// <param name="Tcp"></param>
        public RawTransport(TcpClient Tcp)
        {
            m_Tcp = Tcp;
            m_Channel = Channel.CreateBounded<JObject>(64);
            Completion = ReceiveWorkAsync();
        }

        /// <summary>
        /// Task that completed when the connection closed.
        /// </summary>
        public Task Completion { get; }

        /// <summary>
        /// Receive object packets from the remote host.
        /// </summary>
        /// <returns></returns>
        private async Task ReceiveWorkAsync()
        {
            while (true)
            {
                var Packet = await InternalReceiveAsync();
                if (Packet is null)
                {
                    if (m_Channel.Writer.TryComplete())
                    {
                        try { m_Tcp.Client.Close(); } catch { }
                        try { m_Tcp.Dispose(); } catch { }
                    }

                    break;
                }

                await m_Channel.Writer.WriteAsync(Packet);
            }
        }

        /// <summary>
        /// Receive as much as specified size.
        /// </summary>
        /// <param name="Size"></param>
        /// <returns></returns>
        private async Task<byte[]> InternalReceiveAsync(int Size)
        {
            var Buffer = new ArraySegment<byte>(new byte[Size]);
            while (Buffer.Count > 0)
            {
                int Length;

                try { Length = await m_Tcp.Client.ReceiveAsync(Buffer, SocketFlags.None); }
                catch
                {
                    if (m_Tcp.Client.Connected)
                        continue;

                    return null;
                }

                Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset + Length, Buffer.Count - Length);
            }

            return Buffer.Array;
        }

        /// <summary>
        /// Receive a packet from the remote host.
        /// </summary>
        /// <returns></returns>
        private async Task<JObject> InternalReceiveAsync()
        {
            while (true)
            {
                var LengthBytes = await InternalReceiveAsync(sizeof(int));
                if (LengthBytes is null) break;

                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(LengthBytes);

                var Packet = await InternalReceiveAsync(BitConverter.ToInt32(LengthBytes));
                if (Packet is null) break;

                return Packet.DecodeAsBson();
            }

            return null;
        }

        /// <summary>
        /// Receive a message asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<object> ReceiveAsync()
        {
            while (!m_Channel.Reader.Completion.IsCompleted)
            {
                try
                {
                    var JObject = await m_Channel.Reader.ReadAsync();
                    if (JObject is null)
                        break;

                    var Name = JObject.Value<string>("type");
                    var Body = JObject.Value<JObject>("body");

                    if (!string.IsNullOrWhiteSpace(Name) && Body != null &&
                        TYPE_TABLE.TryGetValue(Name, out var Type))
                    {
                        var Instance = Body.ToObject(Type);
                        
                        try
                        {
                            if (Instance is IPacketCallback Packet)
                                Packet.OnUnpacked();
                        }

                        catch
                        {
                            Kick();
                            break;
                        }

                        return Instance;
                    }
                }

                catch { }
            }

            return null;
        }

        /// <summary>
        /// Kick the connection immediately.
        /// </summary>
        public void Kick()
        {
            try { m_Tcp.Client.Close(); }
            catch { }
        }

        /// <summary>
        /// Send a message asynchronously.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public async Task<EmitStatus> SendAsync(object Message)
        {
            if (Message is null)
                return  EmitStatus.Forbidden;

            var Type = Message.GetType();
            if (Type.Assembly.FullName != ASSEMBLY.FullName)
                return EmitStatus.Forbidden;

            var Tcs = await LockAsync();

            try
            {
                if (!TryGetSocket(out var Socket))
                    return EmitStatus.NotConnected;

                try
                {
                    if (Message is IPacketCallback Callback)
                        Callback.OnPacking();
                }
                catch { return EmitStatus.InvalidPacket; }

                var Packet = new ArraySegment<byte>(JObject.FromObject(Message).EncodeAsBson());
                while(Packet.Count > 0)
                {
                    int Length;

                    try { Length = await Socket.SendAsync(Packet, SocketFlags.None); }
                    catch
                    {
                        if (Socket.Connected)
                            continue;

                        Length = 0;
                    }

                    if (Length <= 0)
                        break;

                    Packet = new ArraySegment<byte>(Packet.Array, Packet.Offset + Length, Packet.Count - Length);
                }

                return Packet.Count <= 0 
                    ? EmitStatus.Success
                    : EmitStatus.Disconnected;
            }
            finally
            {
                Tcs.TrySetResult();
            }
        }

        /// <summary>
        /// Try to get socket instance from <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="Socket"></param>
        /// <returns></returns>
        private bool TryGetSocket(out Socket Socket)
        {
            try
            {
                Socket = m_Tcp.Client;
                return Socket != null && Socket.Connected;
            }

            catch { Socket = null; }
            return false;
        }

        /// <summary>
        /// Lock the transport to transmit the message.
        /// </summary>
        /// <returns></returns>
        private async Task<TaskCompletionSource> LockAsync()
        {
            var Tcs = new TaskCompletionSource();
            while (!m_Channel.Reader.Completion.IsCompleted)
            {
                Task Waiter;
                lock (this)
                {
                    if (m_Tcs != null && !m_Tcs.Task.IsCompleted)
                        Waiter = m_Tcs.Task;

                    else
                    {
                        m_Tcs = Tcs;
                        Waiter = null;
                    }
                }

                if (Waiter != null)
                {
                    await Waiter;
                    continue;
                }

                break;
            }

            return Tcs;
        }
    }
}
