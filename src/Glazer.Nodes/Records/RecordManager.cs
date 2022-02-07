using Glazer.Nodes.Models.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Glazer.Nodes.Records
{
    /// <summary>
    /// Manages all records about the blockchain.
    /// </summary>
    public abstract class RecordManager
    {
        /// <summary>
        /// Put the block to update the record history.
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task PutAsync(Block Block, CancellationToken Token = default);

        /// <summary>
        /// Gets the record by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<RecordColumn[]> GetAsync(RecordKey Key, CancellationToken Token = default);

        /// <summary>
        /// Gets the record by its column key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public abstract Task<RecordColumn> GetAsync(RecordColumnKey Key, CancellationToken Token = default);
    }
}
