using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Notations;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    /// <summary>
    /// Request message to read a blob.
    /// </summary>
    [NodeMessage("glazer_blob_remove.reply")]
    public class RemoveBlobReply : BlobReply, IBinaryMessage
    {
    }
}
