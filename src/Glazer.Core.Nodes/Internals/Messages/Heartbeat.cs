using Glazer.Core.Models.Interfaces;
using Glazer.Core.Notations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Messages
{
    [NodeMessage("heartbeat")]
    internal class Heartbeat : IMessage
    {
        /// <summary>
        /// Time to live.
        /// </summary>
        public int Ttl { get; set; }

        /// <summary>
        /// Endpoint to advertise.
        /// </summary>
        public IPEndPoint Endpoint { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            var Ttl = Math.Min(Math.Max(2, this.Ttl), 32);
            Writer.Write7BitEncodedInt(Ttl);
            Writer.Write(Endpoint.ToString());
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            Ttl = Reader.Read7BitEncodedInt();
            Endpoint = IPEndPoint.Parse(Reader.ReadString());
        }
    }
}
