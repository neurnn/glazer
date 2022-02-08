using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Models.Interfaces;
using Glazer.Nodes.Notations;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts.Storages.Messages
{
    /// <summary>
    /// Request message to read a blob.
    /// </summary>
    [NodeMessage("glazer_blob_get")]
    public class GetBlob : BlobKey, IBinaryMessage
    {
    }
}
