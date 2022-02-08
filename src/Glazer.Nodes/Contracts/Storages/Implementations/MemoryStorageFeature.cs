using Glazer.Nodes.Contracts.Storages.Messages;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts.Storages.Implementations
{
    public class MemoryStorageNode : StorageNode
    {
        private Dictionary<string, (byte[] Data, Guid Tag)> m_Datas = new();

        /// <summary>
        /// Initialize a new <see cref="MemoryStorageNode"/> instance.
        /// </summary>
        /// <param name="Account"></param>
        public MemoryStorageNode(Account Account)
        {
            this.Account = Account;
            SetStatus(NodeStatus.Ready);
        }

        /// <inheritdoc/>
        public override bool IsRemote => false;

        /// <inheritdoc/>
        public override bool IsRemoteInitiated => false;

        /// <inheritdoc/>
        public override Account Account { get; }

        /// <inheritdoc/>
        public override Task<NewBlobReply> NewAsync(NewBlob Request, CancellationToken Token = default)
        {
            var DataKey = $"{Request.ClassId}.{Request.BlobName}";
            var Tag = Guid.NewGuid();

            lock(this)
            {
                if (m_Datas.ContainsKey(DataKey))
                    return Task.FromResult(new NewBlobReply { Status = HttpStatusCode.Forbidden });

                m_Datas[DataKey] = (Request.Data, Tag);
                return Task.FromResult(new NewBlobReply
                {
                    Status = HttpStatusCode.OK,
                    BlobTag = Tag
                });
            }
        }

        /// <inheritdoc/>
        public override Task<GetBlobReply> GetAsync(GetBlob Request, CancellationToken Token = default)
        {
            var DataKey = $"{Request.ClassId}.{Request.BlobName}";
            lock(this)
            {
                if (!m_Datas.TryGetValue(DataKey, out var Tuple))
                    return Task.FromResult(new GetBlobReply { Status = HttpStatusCode.NotFound });

                if (Tuple.Tag == Request.BlobTag && Request.BlobTag != Guid.Empty)
                    return Task.FromResult(new GetBlobReply { Status = HttpStatusCode.NotModified });

                return Task.FromResult(new GetBlobReply
                {
                    Status = HttpStatusCode.OK,
                    Data = Tuple.Data,
                    BlobTag = Tuple.Tag
                });
            }
        }

        /// <inheritdoc/>
        public override Task<SetBlobReply> SetAsync(SetBlob Request, CancellationToken Token = default)
        {
            var DataKey = $"{Request.ClassId}.{Request.BlobName}";
            lock (this)
            {
                if (!m_Datas.TryGetValue(DataKey, out var Tuple))
                    return Task.FromResult(new SetBlobReply { Status = HttpStatusCode.NotFound });

                if (Tuple.Tag != Request.BlobTag && Request.BlobTag != Guid.Empty)
                    return Task.FromResult(new SetBlobReply { Status = HttpStatusCode.Conflict });

                var Tag = Guid.NewGuid();
                m_Datas[DataKey] = (Tuple.Data, Tag);

                return Task.FromResult(new SetBlobReply
                {
                    Status = HttpStatusCode.OK,
                    BlobTag = Tag
                });
            }
        }

        /// <inheritdoc/>
        public override Task<RemoveBlobReply> RemoveAsync(RemoveBlob Request, CancellationToken Token = default)
        {
            var DataKey = $"{Request.ClassId}.{Request.BlobName}";
            lock (this)
            {
                if (!m_Datas.TryGetValue(DataKey, out var Tuple))
                    return Task.FromResult(new RemoveBlobReply { Status = HttpStatusCode.NotFound });

                if (Tuple.Tag != Request.BlobTag && Request.BlobTag != Guid.Empty)
                    return Task.FromResult(new RemoveBlobReply { Status = HttpStatusCode.Conflict });

                m_Datas.Remove(DataKey);
                return Task.FromResult(new RemoveBlobReply
                {
                    Status = HttpStatusCode.OK,
                    BlobTag = Tuple.Tag
                });
            }
        }
    }
}
