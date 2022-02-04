using Glazer.Blockchains.Models;
using Glazer.Blockchains.Models.Interfaces;
using Glazer.Blockchains.Models.Internals;
using Glazer.Blockchains.Packets;
using Glazer.Blockchains.Packets.Authentication;
using Glazer.Blockchains.Transports.Internals;
using Glazer.Core.Cryptography;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Transports
{
    public class Transport : ITransport
    {
        private NodeOptions m_Options;
        private RawTransport m_Raw;
        private Task m_Handshake;
        
        /// <summary>
        /// Initialize a new <see cref="Transport"/> instance for the <see cref="Models.PeerInfo"/>.
        /// </summary>
        /// <param name="Tcp"></param>
        /// <param name="Info"></param>
        public Transport(TcpClient Tcp, PeerInfo Info, NodeOptions Options)
        {
            if (Info.Transport != null)
                throw new InvalidOperationException("Peer has transport.");

            PeerInfo = Info;
            m_Options = Options;
            m_Raw = new RawTransport(Tcp);
            m_Handshake = HandshakeAsync();
        }

        /// <inheritdoc/>
        public Task Completion => m_Raw.Completion;

        /// <inheritdoc/>
        public PeerInfo PeerInfo { get; }

        /// <summary>
        /// Handshake asynchronously.
        /// </summary>
        /// <returns></returns>
        private async Task HandshakeAsync()
        {
            PeerInfo.SetStatus(PeerStatus.Connected); // --> CONNECTED.

            var Status = await m_Raw.SendAsync(Notify_NodeInformation.FromOptions(m_Options));
            if (Status != EmitStatus.Success)
            {
                m_Raw.Kick();
                throw new InvalidOperationException("Failed to notify the node information.");
            }

            var Phrase = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("r") + "/TS:" + m_Options.LoginName);
            var Handshaked = false;

            PeerInfo.SetStatus(PeerStatus.Authenticating); // --> AUTH_ING
            while (true)
            {
                var Successful = false;
                try
                {
                    var Message = await m_Raw.ReceiveAsync();
                    if (Message is null)
                        throw new InvalidOperationException("Failed to handshake.");

                    switch (Message)
                    {
                        case Notify_NodeInformation NNI:
                            if (NNI.ChainId != m_Options.ChainId)
                                throw new InvalidOperationException("Invalid chain-id received.");

                            if (NNI.InitialBlockId != m_Options.InitialBlockId)
                                throw new InvalidOperationException("Invalid genesis block id received.");

                            if ((PeerInfo.PublicKey = NNI.NodePubKey) == PublicKey.Empty)
                                throw new InvalidOperationException("Node can not have `empty` public key.");

                            if (string.IsNullOrWhiteSpace(PeerInfo.Login = NNI.NodeLoginName))
                                throw new InvalidOperationException("Node can not have `empty` login name.");

                            /* Send the authentication request. */
                            await m_Raw.SendAsync(new Request_Authenticate { PhraseToSign = Base58.Encode(Phrase) });
                            break;

                        case Request_Authenticate ReqAuth:
                            if (string.IsNullOrWhiteSpace(ReqAuth.PhraseToSign))
                                throw new InvalidOperationException("Invalid signing phrase.");
                            {
                                var Sign = Secp256k1.Instance.Sign(m_Options.NodeKey, Base58.Decode(ReqAuth.PhraseToSign));
                                await m_Raw.SendAsync(new Response_Authenticate { PhraseSigned = Sign.ToString() });
                            }
                            break;

                        case Response_Authenticate RepAuth:
                            if (string.IsNullOrWhiteSpace(RepAuth.PhraseSigned))
                                throw new InvalidOperationException("Invalid signing phrase.");
                            {
                                var Sign = SignatureValue.Parse(RepAuth.PhraseSigned);
                                if (!Secp256k1.Instance.Verify(Sign, m_Options.NodePublicKey, Phrase))
                                    throw new InvalidOperationException("Invalid signing phrase.");

                                Handshaked = true;
                            }
                            break;

                        default:
                            break;
                    }

                    Successful = true;
                    if (Handshaked)
                    {
                        PeerInfo.Transport = this;
                        PeerInfo.TimeStamp = DateTime.UtcNow;
                        PeerInfo.SetStatus(PeerStatus.Authenticated); // AUTH_ED
                        break;
                    }
                }
                finally
                {
                    if (!Successful)
                    {
                        PeerInfo.Transport = null;
                        PeerInfo.SetStatus(PeerStatus.Disconnecting); // DISCONNECTING

                        m_Raw.Kick();
                        PeerInfo.SetStatus(PeerStatus.Disconnected);  // DISCONNECTED
                        await m_Raw.Completion;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public virtual async Task<EmitStatus> EmitAsync(object Message)
        {
            try { await m_Handshake; }
            catch
            {
                return EmitStatus.Disconnected;
            }

            if (PeerInfo.Transport == this)
            {
                try { return await m_Raw.SendAsync(Message); }
                catch
                {
                    await CleanupAsync();
                }
            }

            return EmitStatus.Failure;
        }

        /// <inheritdoc/>
        public virtual async Task<object> WaitAsync()
        {
            try { await m_Handshake; }
            catch
            {
                return null;
            }

            if (PeerInfo.Transport == this)
            {
                try { return await m_Raw.ReceiveAsync(); }
                catch
                {
                    await CleanupAsync();
                }
            }

            return null;
        }

        /// <summary>
        /// Clean up the transport.
        /// </summary>
        private async Task CleanupAsync()
        {
            lock (this)
            {
                if (PeerInfo.Status == PeerStatus.Authenticated)
                {
                    PeerInfo.SetStatus(PeerStatus.Disconnecting);
                    PeerInfo.Transport = null;

                    m_Raw.Kick();
                }

                PeerInfo.SetStatus(PeerStatus.Disconnected);
            }

            await m_Raw.Completion;
        }
    }
}
