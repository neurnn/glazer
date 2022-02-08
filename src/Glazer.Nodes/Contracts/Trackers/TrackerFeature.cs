using Glazer.Nodes.Contracts.Trackers.Messages;
using Glazer.Nodes.Models.Blocks;
using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Models.Histories;
using Glazer.Nodes.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts.Trackers
{
    public abstract class TrackerNode : NodeFeature
    {
        /// <inheritdoc/>
        public override NodeFeatureType NodeType => NodeFeatureType.Tracker;

        /// <summary>
        /// Put the block to update the record history.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<PutBlockReply> PutAsync(PutBlock Request, CancellationToken Token = default);

        /// <summary>
        /// Gets the record by its key.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<GetRowReply> GetAsync(GetRow Request, CancellationToken Token = default);

        /// <summary>
        /// Gets the record by its column key.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<GetColumnReply> GetAsync(GetColumn Request, CancellationToken Token = default);

        /// <inheritdoc/>
        public override async Task<NodeResponse> ExecuteAsync(NodeRequest Request)
        {
            object Response;
            switch (Request.Message)
            {
                case PutBlock New:
                    Response = await PutAsync(New, Request.Aborted);
                    break;

                case GetRow Get:
                    Response = await GetAsync(Get, Request.Aborted);
                    break;

                case GetColumn Set:
                    Response = await GetAsync(Set, Request.Aborted);
                    break;

                default:
                    return null;
            }

            return new NodeResponse { Message = Response };
        }
    }
}
