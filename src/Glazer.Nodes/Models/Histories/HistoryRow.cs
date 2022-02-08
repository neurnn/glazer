using Glazer.Nodes.Records;

namespace Glazer.Nodes.Models.Histories
{
    public struct HistoryRow
    {
        public HistoryRow(HistoryRowKey Key, HistoryColumn[] Columns)
        {
            this.Key = Key;
            this.Columns = Columns;
        }

        /// <summary>
        /// Indicates whether the history row is empty or not.
        /// </summary>
        public bool IsEmpty => Key.IsNull || Columns == null || Columns.Length <= 0;

        /// <summary>
        /// History Key.
        /// </summary>
        public HistoryRowKey Key { get; }

        /// <summary>
        /// Columns that included in the row.
        /// </summary>
        public HistoryColumn[] Columns { get; }
    }
}
