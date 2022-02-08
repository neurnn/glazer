using Glazer.Nodes.Models.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    /// <summary>
    /// Request that has target information.
    /// </summary>
    public class BlobKey : IBinaryMessage
    {
        /// <summary>
        /// Category of the target blob.
        /// </summary>
        public Guid ClassId { get; set; }

        /// <summary>
        /// Blob Name to read.
        /// </summary>
        public string BlobName { get; set; }

        /// <summary>
        /// Blob Tag to avoid the race condition.
        /// Default: <see cref="Guid.Empty"/> (all access).
        /// </summary>
        public Guid BlobTag { get; set; }

        /// <inheritdoc/>
        public virtual void Decode(BinaryReader Reader)
        {
            ClassId = new Guid(Reader.ReadBytes(16));
            BlobTag = new Guid(Reader.ReadBytes(16));
            BlobName = Reader.ReadString();
        }

        /// <inheritdoc/>
        public virtual void Encode(BinaryWriter Writer)
        {
            Writer.Write(ClassId.ToByteArray());
            Writer.Write(BlobTag.ToByteArray());
            Writer.Write(BlobName);
        }
    }

}
