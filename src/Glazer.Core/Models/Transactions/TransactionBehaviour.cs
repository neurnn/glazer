using Backrole.Crypto;
using Glazer.Core.Records;
using System;
using System.Collections.Generic;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Transactions
{
    public class TransactionBehaviour
    {
        /// <summary>
        /// Code Id to execute.
        /// Executors should have `[Account]@[CodeId]` record data.
        /// </summary>
        public Guid CodeId { get; set; }

        /// <summary>
        /// Code Name that defined on the real code.
        /// </summary>
        public string CodeName { get; set; }

        /// <summary>
        /// Arguments that encoded as BSON.
        /// </summary>
        public byte[] CodeArgs { get; set; }
    }
}
