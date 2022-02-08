using Glazer.Nodes.Contracts.Storages.Messages;
using Glazer.Nodes.Exceptions;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Notations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts.Storages
{
    /// <summary>
    /// Storage Driver.
    /// </summary>
    public abstract class StorageFeature : NodeFeature
    {
        /// <summary>
        /// Node features.
        /// </summary>
        public override NodeFeatureType NodeType { get; } = NodeFeatureType.Storage;

        /// <summary>
        /// Creates a new blob asynchronously.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<NewBlobReply> NewAsync(NewBlob Request, CancellationToken Token = default);

        /// <summary>
        /// Gets a blob asynchronously.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public abstract Task<GetBlobReply> GetAsync(GetBlob Request, CancellationToken Token = default);

        /// <summary>
        /// Set blob data asynchronously
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<SetBlobReply> SetAsync(SetBlob Request, CancellationToken Token = default);

        /// <summary>
        /// Remove a blob asynchronously.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<RemoveBlobReply> RemoveAsync(RemoveBlob Request, CancellationToken Token = default);

        /// <inheritdoc/>
        public override async Task<NodeResponse> ExecuteAsync(NodeRequest Request)
        {
            object  Response;
            switch (Request.Message)
            {
                case NewBlob New:
                    Response = await NewAsync(New, Request.Aborted);
                    break;

                case GetBlob Get:
                    Response = await GetAsync(Get, Request.Aborted);
                    break;

                case SetBlob Set:
                    Response = await SetAsync(Set, Request.Aborted);
                    break;

                case RemoveBlob Remove:
                    Response = await RemoveAsync(Remove, Request.Aborted);
                    break;

                default:
                    return null;
            }

            return new NodeResponse { Message = Response };
        }

    }
}
