using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Notations;
using System.IO;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    /// <summary>
    /// Request message to update a blob.
    /// </summary>
    [NodeMessage("glazer_blob_set")]
    public class SetBlob : BlobKey, IBinaryMessage
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
