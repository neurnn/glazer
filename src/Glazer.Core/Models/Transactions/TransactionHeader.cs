using Backrole.Crypto;
using System;

namespace Glazer.Core.Models.Transactions
{
    /// <summary>
    /// Indicates the head informations of the transaction.
    /// </summary>
    public class TransactionHeader
    {
        /// <summary>
        /// Transaction Model.
        /// This will be set when the header instance assigned on the transaction.
        /// </summary>
        public Transaction Transaction { get; internal set; }

        /// <summary>
        /// Version Number.
        /// </summary>
        public uint Version { get; set; } = 0;

        /// <summary>
        /// Transaction Id.
        /// </summary>
        public HashValue TrxId { get; set; }

        /// <summary>
        /// Transaction Time Stamp in Utc.
        /// </summary>
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Sender Account information.
        /// </summary>
        public Account Sender { get; set; }

        /// <summary>
        /// Sender Seal to verify the transaction is from the sender.
        /// </summary>
        public SignValue SenderSeal { get; set; }

    }

}
