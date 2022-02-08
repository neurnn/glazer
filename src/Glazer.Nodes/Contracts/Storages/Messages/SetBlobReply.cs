using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Notations;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    [NodeMessage("glazer_blob_get.reply")]
    public class SetBlobReply : BlobReply, IBinaryMessage
    {
    }
}
