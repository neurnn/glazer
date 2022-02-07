using Backrole.Crypto;
using Glazer.Nodes.Records;
using System.Collections.Generic;
using static Glazer.Nodes.Helpers.ModelHelpers;

namespace Glazer.Nodes.Models.Transactions
{
    public class TransactionBehaviour
    {
        private Dictionary<RecordColumnKey, byte[]> m_CodeExpects;

        /// <summary>
        /// Code Id to execute.
        /// Executors should have `[Account]@[CodeId]` record data.
        /// </summary>
        public HashValue CodeId { get; set; }

        /// <summary>
        /// Code Name that defined on the real code.
        /// </summary>
        public string CodeName { get; set; }

        /// <summary>
        /// Arguments that encoded as BSON.
        /// </summary>
        public byte[] CodeArgs { get; set; }

        /// <summary>
        /// Expected Results.
        /// Pairs that points a slot.
        /// Empty value is about removal of the slot, otherwise alter(or create).
        /// </summary>
        public Dictionary<RecordColumnKey, byte[]> CodeExpects
        {
            get => Ensures(ref m_CodeExpects);
            set => Assigns(ref m_CodeExpects, value);
        }
    }
}
