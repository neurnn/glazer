using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Common;
using Glazer.P2P.Abstractions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.P2P.Tcp.Internals
{
    internal class Connection : IConnection
    {
        public const int CurrentProtocolVersion = 1;

        private TcpMessanger m_Messanger;
        private Socket m_Socket;

        private IPEndPoint m_Endpoint;

        /// <summary>
        /// Initialize a new <see cref="Connection"/> instance.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Socket"></param>
        public Connection(TcpMessanger Messanger, Socket Socket)
        {
            m_Messanger = Messanger;
            m_Endpoint = (m_Socket = Socket).RemoteEndPoint as IPEndPoint;
        }

        /// <inheritdoc/>
        public SignPublicKey PublicKey { get; private set; }

        /// <summary>
        /// Run the connection loop.
        /// </summary>
        /// <param name="Channel"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task RunAsync(ChannelWriter<Message> Channel, CancellationToken Token)
        {
            var Pool = m_Messanger.GetPool();

            if (await HandshakeAsync(Pool, Token))
            {
                try
                {
                    m_Messanger.NotifyPeerEvent(PublicKey, true);
                    await HandleReceive(Channel, Token);
                }

                finally
                {
                    Pool.Remove(this);
                    m_Messanger.NotifyPeerEvent(PublicKey, false);
                }
            }

            try { m_Socket.Close(); } catch { }
            try { m_Socket.Dispose(); } catch { }
        }

        /// <summary>
        /// Handshake .
        /// </summary>
        /// <param name="Pool"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<bool> HandshakeAsync(ConnectionPool Pool, CancellationToken Token)
        {
            try
            {
                int Stage = 0;
                while (true)
                {
                    var Reader = await m_Socket.ReceivePacketAsync(Token);
                    if (Reader is null)
                        break;

                    /**
                     1. Client => Server (Version, Public Key).
                     2. Server => Client (Server Signature: digest; client's public key).
                     3. Client => Server (Client Signature: digest; server's public key).
                     */
                    switch (Stage)
                    {
                        case 0: // -> Initial Packet.
                            {
                                using (Reader)
                                {
                                    if (Reader.Read7BitEncodedInt() > CurrentProtocolVersion)
                                        break;

                                    if ((PublicKey = Reader.ReadSignPublicKey()) == m_Messanger.KeyPair.PublicKey)
                                        return false;
                                }

                                using (var Writer = new PacketWriter())
                                {
                                    /* Send the client public key's signature that signed by server key. */
                                    Writer.Write(m_Messanger.KeyPair.SignSeal(PublicKey.Value));
                                    Emit(Writer);
                                }

                                Stage++;
                            }
                            continue;

                        case 1:
                            {
                                /* Verify the client signature that signs the server key as digest. */
                                var Signature = new SignSealValue(Reader.ReadSignValue(), PublicKey);
                                if (!Signature.Verify(m_Messanger.KeyPair.PublicKey.Value))
                                    return false;
                            }
                            break;
                    }

                    if (!Pool.Check(this))
                        return Pool.Add(this);

                    break;
                }
            }

            catch { }
            return false;
        }

        /// <summary>
        /// Handle the receive channel.
        /// </summary>
        /// <param name="Channel"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task HandleReceive(ChannelWriter<Message> Channel, CancellationToken Token)
        {
            while (true)
            {
                PacketReader Reader;
                try { Reader = await m_Socket.ReceivePacketAsync(Token); }
                catch
                {
                    break;
                }

                if (Reader is null)
                    break;

                try
                {
                    var Message = new Message();

                    Message.Endpoint = m_Endpoint;
                    Message.Decode(Reader, true);

                    if (Message.Expiration <= TimeStamp.Now)
                        continue;

                    /* Pushes only valid messages. */
                    if (Message.Sender.IsValid && Message.Verify())
                        await Channel.WriteAsync(Message, Token);
                }
                catch { break; }
                finally { Reader.Dispose(); }
            }
        }

        /// <inheritdoc/>
        public void Emit(PacketWriter Packet)
        {
            try
            {
                m_Socket
                   .SendPacketAsync(Packet)
                   .GetAwaiter().GetResult();
            }
            catch { }
        }
    }
}
