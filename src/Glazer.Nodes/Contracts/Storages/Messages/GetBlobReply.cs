using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Notations;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    /// <summary>
    /// Request message to read a blob.
    /// </summary>
    [NodeMessage("glazer_blob_get.reply")]
    public class GetBlobReply : BlobReply, IBinaryMessage
    {
        /// <summary>
        /// Data to write.
        /// </summary>
        public byte[] Data { get; set; }

        /// <inheritdoc/>
        public override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write7BitEncodedInt((int)Status);
            Writer.WriteFrame(Data);
        }

        /// <inheritdoc/>
        public override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Status = (HttpStatusCode)Reader.Read7BitEncodedInt();
            Data = Reader.ReadFrame();
        }
    }
}
