using Glazer.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer
{
    public static class TcpHelpers
    {
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);
        private static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        /// Receive bytes from <see cref="TcpClient"/> instance.
        /// </summary>
        /// <param name="Tcp"></param>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<byte[]> ReceiveFulfilledAsync(this Socket Tcp, int Length, CancellationToken Token = default)
        {
            var Buffer = new ArraySegment<byte>(new byte[Length]);
            while (Buffer.Count > 0)
            {
                try { Length = await Tcp.ReceiveAsync(Buffer, SocketFlags.None, Token); }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                    if (Tcp.Connected)
                        continue;

                    Length = 0;
                }

                if (Length <= 0)
                    return null;

                Buffer = new ArraySegment<byte>(Buffer.Array,
                    Buffer.Offset + Length, Buffer.Count - Length);
            }

            return Buffer.Array;
        }

        /// <summary>
        /// Receive the <see cref="PacketReader"/> from <see cref="TcpClient"/> instance.
        /// </summary>
        /// <param name="Tcp"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<PacketReader> ReceivePacketAsync(this Socket Tcp, CancellationToken Token = default)
        {
            while(true)
            {
                var Temp = await Tcp.ReceiveFulfilledAsync(sizeof(int), Token);
                if (Temp is null) return null;

                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(Temp);

                var Length = Math.Max(BitConverter.ToInt32(Temp), 0);
                if (Length > 0)
                {
                    var Packet = await Tcp.ReceiveFulfilledAsync(Length);
                    if (Packet is null) return null;
                    return new PacketReader(Packet);
                }
            }
        }

        /// <summary>
        /// Send bytes to <see cref="TcpClient"/> instance.
        /// </summary>
        /// <param name="Tcp"></param>
        /// <param name="Buffer"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static Task<bool> SendFulfilledAsync(this Socket Tcp, ArraySegment<byte> Buffer, CancellationToken Token = default)
        {
            return Task.Run(() =>
            {
                Token.ThrowIfCancellationRequested();
                lock (Tcp)
                {
                    while (Buffer.Count > 0)
                    {
                        int Length;
                        try { Length = Tcp.Send(Buffer.Array, Buffer.Offset, Buffer.Count, SocketFlags.None); }
                        catch
                        {
                            if (Tcp.Connected)
                                continue;

                            Length = 0;
                        }

                        if (Length <= 0)
                            break;

                        Buffer = new ArraySegment<byte>(Buffer.Array,
                            Buffer.Offset + Length, Buffer.Count - Length);
                    }
                }

                return Buffer.Count <= 0;
            });
        }

        /// <summary>
        /// Send the <see cref="PacketWriter"/> to the <see cref="TcpClient"/> instance.
        /// </summary>
        /// <param name="Tcp"></param>
        /// <param name="Writer"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<bool> SendPacketAsync(this Socket Tcp, PacketWriter Writer, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            var Packet = Writer.ToByteArray();
            var Temp = BitConverter.GetBytes(Packet.Length);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Temp);

            Array.Resize(ref Packet, Packet.Length + Temp.Length);
            Buffer.BlockCopy(Packet, 0, Packet, Temp.Length, Packet.Length - Temp.Length);
            Buffer.BlockCopy(Temp, 0, Packet, 0, Temp.Length);
            return await SendFulfilledAsync(Tcp, Packet, Token);
        }
    }
}
