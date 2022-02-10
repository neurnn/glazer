using Glazer.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Helpers
{
    internal static class SocketHelpers
    {
        /// <summary>
        /// Receive bytes from the socket and returns received array.
        /// If the connection lost, this returns null not exception.
        /// </summary>
        /// <param name="Socket"></param>
        /// <param name="Length"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private static async Task<byte[]> ReceiveBytes(this Socket Socket, int Length, CancellationToken Token)
        {
            var Buffer = new ArraySegment<byte>(new byte[Length]);
            while(Buffer.Count > 0)
            {
                int ReceivedBytes;

                try { ReceivedBytes = await Socket.ReceiveAsync(Buffer, SocketFlags.None, Token); }
                catch
                {
                    if (!Token.IsCancellationRequested && Socket.Connected)
                        continue;

                    ReceivedBytes = 0;
                }

                if (ReceivedBytes <= 0)
                    return null;

                Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset + ReceivedBytes, Buffer.Count - ReceivedBytes);
            }

            return Buffer.Array;
        }

        /// <summary>
        /// Receive bytes from the socket and returns transformed array.
        /// If the connection lost, this returns null not exception.
        /// </summary>
        /// <param name="Socket"></param>
        /// <param name="Length"></param>
        /// <param name="Transform"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private static async Task<byte[]> ReceiveBytes(this Socket Socket, int Length, Func<byte[], byte[]> Transform, CancellationToken Token)
        {
            var Buffer = await Socket.ReceiveBytes(Length, Token);
            if (Buffer != null)
            {
                return Transform(Buffer);
            }

            return null;
        }

        /// <summary>
        /// Receive bytes in chunk encoding.
        /// </summary>
        /// <param name="Socket"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private static async Task<byte[]> ReceiveChunked(this Socket Socket, CancellationToken Token)
        {
            while (!Token.IsCancellationRequested)
            {
                var LengthBytes = await Socket.ReceiveBytes(sizeof(int),
                    Bytes =>
                    {
                        if (BitConverter.IsLittleEndian)
                            return Bytes;

                        Array.Reverse(Bytes);
                        return Bytes;
                    }, Token);

                if (LengthBytes is null)
                    return null;

                var Length = BitConverter.ToInt32(LengthBytes);
                if (Length <= 0)
                    continue;

                return await Socket.ReceiveBytes(Length, Token);
            }

            return null;
        }

        /// <summary>
        /// Receive chunked message asynchronously.
        /// If the connection lost, this returns null not exception.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<BinaryReader> ReceiveChunkedAsync(this Socket Socket, CancellationToken Token)
        {
            var Data = await Socket.ReceiveChunked(Token);
            if (Data != null)
                return new EndianessReader(new MemoryStream(Data, false), true);

            return null;
        }
    }
}
