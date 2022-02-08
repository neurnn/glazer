using Glazer.Nodes.Models.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    public class BlobReply : IBinaryMessage
    {
        /// <summary>
        /// Blob Tag to avoid the race condition.
        /// Default: <see cref="Guid.Empty"/> (all access).
        /// </summary>
        public Guid BlobTag { get; set; }

        /// <summary>
        /// Response Code.
        /// </summary>
        public HttpStatusCode Status { get; set; }

        /// <inheritdoc/>
        public virtual void Decode(BinaryReader Reader)
        {
            BlobTag = new Guid(Reader.ReadBytes(16));
            Status = (HttpStatusCode)Reader.Read7BitEncodedInt();
        }

        /// <inheritdoc/>
        public virtual void Encode(BinaryWriter Writer)
        {
            Writer.Write(BlobTag.ToByteArray());
            Writer.Write7BitEncodedInt((int)Status);
        }
    }

}
