using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Glazer.P2P.Abstractions
{
    public sealed class Message
    {
        private Dictionary<string, string> m_Headers;

        /// <summary>
        /// Remote Endpoint.
        /// </summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>
        /// Sender's Signature and Public Key.
        /// </summary>
        public SignSealValue Sender { get; set; }

        /// <summary>
        /// Receiver's Public Key.
        /// </summary>
        public SignPublicKey Receiver { get; set; }

        /// <summary>
        /// Message Headers.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get => ModelHelpers.OnDemand(ref m_Headers);
            set => m_Headers = value;
        }

        /// <summary>
        /// Expiration of the message.
        /// </summary>
        public TimeStamp Expiration { get; set; } = DateTime.Now.AddSeconds(10);

        /// <summary>
        /// Type of the message.
        /// </summary>
        public string Type
        {
            get
            {
                Headers.TryGetValue("Type", out var Type);
                return Type;
            }

            set => Headers["Type"] = value;
        }

        /// <summary>
        /// Message Data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Encode the message to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(Expiration);

            Writer.Write(Sender);
            Writer.Write(Receiver);

            if (m_Headers is null)
                Writer.Write7BitEncodedInt(0);

            else
            {
                Writer.Write7BitEncodedInt(m_Headers.Count);
                foreach(var Each in m_Headers)
                {
                    Writer.Write(Each.Key);
                    Writer.Write(Each.Value);
                }
            }

            if (Data is null)
                Writer.Write7BitEncodedInt(0);

            else
            {
                Writer.Write7BitEncodedInt(Data.Length);
                Writer.Write(Data);
            }
        }

        /// <summary>
        /// Decode the message from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        public bool Decode(BinaryReader Reader, bool Enforce = false)
        {
            if ((Expiration = Reader.ReadTimeStamp()) > TimeStamp.Now || Enforce)
            {
                Sender = Reader.ReadSignSealValue();
                Receiver = Reader.ReadSignPublicKey();

                {
                    var Count = Reader.Read7BitEncodedInt();
                    for (var i = 0; i < Count; ++i)
                    {
                        var Key = Reader.ReadString();
                        Headers[Key] = Reader.ReadString();
                    }
                }

                Data = Reader.ReadBytes(Reader.Read7BitEncodedInt());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sign the message using the key pair.
        /// </summary>
        /// <param name="KeyPair"></param>
        /// <returns></returns>
        public Message Sign(SignKeyPair KeyPair, bool Enforce = false)
        {
            if (Sender.IsValid && !Enforce)
                return this;

            Sender = default;
            using(var Writer = new PacketWriter())
            {
                Encode(Writer);
                Sender = KeyPair.SignSeal(Writer.ToByteArray());
            }

            return this;
        }

        /// <summary>
        /// Verify the message using <see cref="Sender"/>'s signature.
        /// </summary>
        /// <returns></returns>
        public bool Verify()
        {
            if (Sender.IsValid)
            {
                var Temp = Sender; Sender = default;
                using (var Writer = new PacketWriter())
                {
                    Encode(Writer);
                    return (Sender = Temp).Verify(Writer.ToByteArray());
                }
            }

            return false;
        }
    }
}
