using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Notations;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    /// <summary>
    /// Request message to create a blob.
    /// </summary>
    [NodeMessage("glazer_blob_new")]
    public class NewBlob : BlobKey, IBinaryMessage
    {
        /// <summary>
        /// Data to write.
        /// </summary>
        public byte[] Data { get; set; }

        /// <inheritdoc/>
        public override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.WriteFrame(Data);
        }

        /// <inheritdoc/>
        public override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Data = Reader.ReadFrame();
        }
    }
}
