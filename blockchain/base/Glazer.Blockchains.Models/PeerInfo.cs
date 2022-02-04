using Glazer.Blockchains.Models.Interfaces;
using Glazer.Blockchains.Models.Internals;
using Glazer.Core.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models
{
    public sealed class PeerInfo : IEncodable
    {
        /// <summary>
        /// Login of the node itself.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Public Key of the node.
        /// </summary>
        public PublicKey PublicKey { get; set; }

        /// <summary>
        /// Time Stamp when the peer connected.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// IP Endpoint of the node.
        /// </summary>
        public IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// Transport instance.
        /// Assigned before the <see cref="Status"/> is set as <see cref="PeerStatus.Authenticated"/>.
        /// And unassigned after the <see cref="Status"/> is set as <see cref="PeerStatus.Disconnecting"/>.
        /// </summary>
        public ITransport Transport { get; set; }

        /// <summary>
        /// Runtime Status (will not be serialized)
        /// </summary>
        public PeerStatus Status { get; private set; } = PeerStatus.Created;

        /// <summary>
        /// Event that broadcasts about the status changes.
        /// </summary>
        public event Action<PeerInfo, PeerStatus> StatusChanged;

        /// <summary>
        /// Set the peer status.
        /// </summary>
        /// <param name="Status"></param>
        /// <returns></returns>
        public void SetStatus(PeerStatus Status)
        {
            if (this.Status != Status)
            {
                this.Status = Status;
                StatusChanged?.Invoke(this, Status);
            }
        }

        /// <summary>
        /// Encode the <see cref="PeerInfo"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Options"></param>
        public void Encode(BinaryWriter Writer, NodeOptions Options)
        {
            Writer.Write(Login);
            Writer.Write(PublicKey.ToString());
            Writer.Write(TimeStamp.ToSeconds(Options.Epoch));

            Writer.Write(EndPoint.Address.ToString());
            Writer.Write(EndPoint.Port);
        }

        /// <summary>
        /// Decode the <see cref="PeerInfo"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Options"></param>
        public void Decode(BinaryReader Reader, NodeOptions Options)
        {
            Login = Reader.ReadString();
            PublicKey = PublicKey.Parse(Reader.ReadString());
            TimeStamp = Reader.ReadDouble().ToDateTime(Options.Epoch);

            var Address = IPAddress.Parse(Reader.ReadString());
            EndPoint = new IPEndPoint(Address, Reader.ReadInt32());
        }
    }
}
