using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Common;
using Glazer.P2P.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.P2P.Tcp.Internals
{
    internal class ContactConnection : IConnection
    {
        private const int ConnectionRecovery = 30;

        private Socket m_Socket;
        private TcpMessanger m_Messanger;
        private IPEndPoint m_Endpoint;
        private int m_Retries = 0;

        /// <summary>
        /// Initialize a new <see cref="ContactConnection"/> instance.
        /// </summary>
        /// <param name="Endpoint"></param>
        public ContactConnection(TcpMessanger Messanger, IPEndPoint Endpoint)
        {
            m_Messanger = Messanger;
            m_Endpoint = Endpoint;
        }

        /// <inheritdoc/>
        public SignPublicKey PublicKey { get; private set; }

        /// <summary>
        /// Original Public Key.
        /// After setting this value, the connection recovery will be performed in every <see cref="ConnectionRecovery"/> seconds.
        /// </summary>
        internal SignPublicKey OriginalPublicKey { get; private set; }

        /// <summary>
        /// Run the connection loop.
        /// </summary>
        /// <param name="Channel"></param>
        /// <returns></returns>
        public async Task RunAsync(ChannelWriter<Message> Channel)
        {
            var Contacts = m_Messanger.GetContacts();

            if (!await ConnectAsync())
                return;

            var Pool = m_Messanger.GetPool();
            if (await HandshakeAsync(Pool))
            {
                try
                {
                    m_Messanger.NotifyPeerEvent(PublicKey, true);
                    await HandleReceive(Channel);
                }
                finally
                {
                    Pool.Remove(this);

                    m_Messanger.NotifyPeerEvent(PublicKey, false);
                }
            }

            try { m_Socket.Close(); } catch { }
            try { m_Socket.Dispose(); } catch { }

            Contacts?.Unset(m_Endpoint);
        }

        /// <summary>
        /// Connect to the remote host.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ConnectAsync()
        {
            while (true)
            {
                m_Socket = new Socket(
                    m_Endpoint.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                using var Cts = new CancellationTokenSource(TimeSpan.FromSeconds(ConnectionRecovery));
                try { await m_Socket.ConnectAsync(m_Endpoint, Cts.Token); }
                catch
                {
                    try { m_Socket.Dispose(); }
                    catch
                    {

                    }

                    m_Socket = null;
                    if (++m_Retries > 3)
                        return false;

                    await Task.Delay(1000);
                    continue;
                }

                m_Retries = 0;
                return true;
            }
        }

        /// <summary>
        /// Handshake with the client.
        /// </summary>
        /// <param name="Pool"></param>
        /// <returns></returns>
        private async Task<bool> HandshakeAsync(ConnectionPool Pool)
        {
            try
            {
                /* Send the initial packet (C to S) */
                using (var Writer = new PacketWriter())
                {
                    Writer.Write7BitEncodedInt(Connection.CurrentProtocolVersion);
                    Writer.Write(m_Messanger.KeyPair.PublicKey);
                    Emit(Writer);
                }

                var Reader = await m_Socket.ReceivePacketAsync();
                if (Reader is null)
                    return false;

                var Signature = Reader.ReadSignSealValue();

                /* Verify the server signature that signs the client key as digest. */
                if (!Signature.Verify(m_Messanger.KeyPair.PublicKey.Value))
                    return false;

                using (var Writer = new PacketWriter())
                {
                    /* Finally, sends the server public key's signature that signed by client key. */
                    Writer.Write(m_Messanger.KeyPair.Sign(Signature.PublicKey.Value));
                    Emit(Writer);
                }

                /* SELF SUICIDE (-_-;;) */
                if ((PublicKey = Signature.PublicKey) == m_Messanger.KeyPair.PublicKey)
                    return false;

                if (!OriginalPublicKey.IsValid)
                     OriginalPublicKey = PublicKey;

                else if (OriginalPublicKey != PublicKey)
                    return false;

                return !Pool.Check(this) && Pool.Add(this);
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
        private async Task HandleReceive(ChannelWriter<Message> Channel)
        {
            while (true)
            {
                PacketReader Reader;
                try { Reader = await m_Socket.ReceivePacketAsync(); }
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
                        await Channel.WriteAsync(Message);
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
