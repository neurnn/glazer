using Glazer.Nodes.Contracts.Trackers.Messages;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Contracts;
using Glazer.Nodes.Models.Histories;
using Glazer.Nodes.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Contracts.Trackers.Implementations
{
    public class MemoryTrackerFeature : TrackerFeature
    {
        private Dictionary<string, Dictionary<string, HistoryColumn>> m_Rows = new(); // Login Name + Code Id : Record Row.

        /// <summary>
        /// Initialize a new <see cref="MemoryTrackerFeature"/> instance.
        /// </summary>
        /// <param name="Account"></param>
        public MemoryTrackerFeature(Account Account)
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
        public override Task<PutBlockReply> PutAsync(PutBlock Request, CancellationToken Token = default)
        {
            var Behaviours = Request.Block.Transactions.SelectMany(
                X => X.Behaviours.SelectMany(Y => X.Behaviours.Select(Z => (Trx: X.Header, Changes: Z))));

            var Keys = new List<HistoryColumnKey>();
            var Reply = new PutBlockReply();
            lock (m_Rows)
            {
                foreach (var Action in Behaviours)
                {
                    var Trx = Action.Trx;

                    foreach (var ColumnKey in Action.Changes.CodeExpects.Keys)
                    {
                        var Blob = Action.Changes.CodeExpects[ColumnKey];
                        var RowKey = ColumnKey.RowKey.ToString();
                        var Column = ColumnKey.Column;

                        if (!m_Rows.TryGetValue(RowKey, out var Columns))
                             m_Rows[RowKey] = Columns = new Dictionary<string, HistoryColumn>();

                        Columns[Column] = new HistoryColumn(
                            ColumnKey, Trx.TimeStamp, Request.Block.Header.Index, Trx.TrxId, Blob
                        );

                        if (Keys.Contains(ColumnKey))
                            continue;

                        Keys.Add(ColumnKey);
                    }
                }
            }

            Reply.Keys = Keys.ToArray();
            Reply.Status = HttpStatusCode.OK;
            return Task.FromResult(Reply);
        }

        /// <inheritdoc/>
        public override Task<GetRowReply> GetAsync(GetRow Request, CancellationToken Token = default)
        {
            if (Request.RowKey.IsNull)
                return Task.FromResult(new GetRowReply());

            var RowKey = Request.RowKey.ToString();
            lock (m_Rows)
            {
                if (!m_Rows.TryGetValue(RowKey, out var RowSlot))
                    return Task.FromResult(new GetRowReply());

                return Task.FromResult(new GetRowReply
                {
                    Row = new HistoryRow(Request.RowKey, RowSlot.Values.ToArray())
                });
            }
        }

        /// <inheritdoc/>
        public override Task<GetColumnReply> GetAsync(GetColumn Request, CancellationToken Token = default)
        {
            if (Request.ColumnKey.IsNull)
                return Task.FromResult(new GetColumnReply());

            var RowKey = Request.ColumnKey.RowKey.ToString();
            var ColumnName = Request.ColumnKey.Column;
            lock (m_Rows)
            {
                if (!m_Rows.TryGetValue(RowKey, out var RowSlot))
                    return Task.FromResult(new GetColumnReply());

                if (!RowSlot.TryGetValue(ColumnName, out var Column))
                    return Task.FromResult(new GetColumnReply());

                return Task.FromResult(new GetColumnReply { Column = Column });
            }
        }

    }
}
