using Backrole.Crypto;
using Glazer.Common.Common;
using Glazer.P2P.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace Glazer.P2P.Protocols
{
    /// <summary>
    /// Invites other peers on the P2P network.
    /// </summary>
    public class InvitePeers : IMessangerProtocol
    {
        private static readonly string TYPE_P2P_INVITE = "p2p.invite";

        /// <summary>
        /// Emit the <see cref="InvitePeers"/> message to all peers.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Expiration">In seconds.</param>
        public static void Emit(IMessanger Messanger, long Expiration = 5)
        {
            var PortBytes = BitConverter
                .GetBytes((ushort)Messanger.Endpoint.Port);

            if (!BitConverter.IsLittleEndian) 
                Array.Reverse(PortBytes);

            Messanger.Emit(new Message
            {
                Type = TYPE_P2P_INVITE,
                Expiration = TimeStamp.Now + Expiration,
                Data = PortBytes
            });
        }

        /// <summary>
        /// Emit the <see cref="InvitePeers"/> message to specific receiver.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Receiver"></param>
        /// <param name="Expiration"></param>
        public static void Emit(IMessanger Messanger, SignPublicKey Receiver, long Expiration = 5)
        {
            if (!Messanger.IsConnectedDirectly(Receiver))
            {
                var PortBytes = BitConverter
                    .GetBytes((ushort)Messanger.Endpoint.Port);

                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(PortBytes);

                Messanger.Emit(new Message
                {
                    Type = TYPE_P2P_INVITE,
                    Expiration = TimeStamp.Now + Expiration,
                    Receiver = Receiver,
                    Data = PortBytes
                });
            }
        }

        /// <summary>
        /// Handle the protocol message.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public bool Handle(IMessanger Messanger, Message Message)
        {
            var Type = Message.Type;

            if (!string.IsNullOrWhiteSpace(Type) &&
                Type.Equals(TYPE_P2P_INVITE, StringComparison.OrdinalIgnoreCase))
            {
                if (Message.Data.Length == sizeof(ushort)) // -> Port number only message.
                {
                    var Address = Encoding.UTF8.GetBytes(Message.Endpoint.Address.ToString());
                    var PortBytes = Message.Data.ToArray();

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(PortBytes);

                    var Port = BitConverter.ToUInt16(PortBytes);
                    Message.Data = Message.Data.Concat(Address).ToArray();
                    Message.Sign(Messanger.KeyPair, true); // -> Re-sign the message.
                    Messanger.Emit(Message); // --> Emit to other hosts.

                    if (!Message.Receiver.IsValid || Message.Receiver == Messanger.KeyPair.PublicKey)
                        Messanger.Contact(new IPEndPoint(Message.Endpoint.Address, Port));
                }

                else
                {
                    if (!Message.Receiver.IsValid)
                         Messanger.Emit(Message); // --> Emit to other hosts.

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(Message.Data, 0, 2);

                    if (!Message.Receiver.IsValid || Message.Receiver == Messanger.KeyPair.PublicKey)
                    {
                        try
                        {
                            var AddressStr = Encoding.UTF8.GetString(Message.Data, 2, Message.Data.Length - 2);
                            var Address = IPAddress.Parse(AddressStr);
                            var Port = BitConverter.ToUInt16(Message.Data, 0);
                            Messanger.Contact(new IPEndPoint(Address, Port));
                        }

                        catch { }
                    }
                }

                return true;
            }

            return false;
        }
    }
}
