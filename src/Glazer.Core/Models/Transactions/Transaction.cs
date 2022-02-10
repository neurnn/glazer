using Backrole.Crypto;
using Glazer.Core.Records;
using System.Collections.Generic;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Transactions
{
    public class Transaction
    {
        private TransactionHeader m_Header;
        private TransactionWitness m_Witness;
        private List<TransactionBehaviour> m_Behaviours;
        private Dictionary<HistoryColumnKey, byte[]> m_ExpectedRecords;


        /// <summary>
        /// Transaction Header.
        /// </summary>
        public TransactionHeader Header
        {
            get => Ensures(ref m_Header, X => X.Transaction = this);
            set => Assigns(ref m_Header, value).Transaction = this;
        }

        /// <summary>
        /// Transation Witness.
        /// </summary>
        public TransactionWitness Witness
        {
            get => Ensures(ref m_Witness, X => X.Transaction = this);
            set => Assigns(ref m_Witness, value).Transaction = this;
        }

        /// <summary>
        /// Get the precondition status of the transaction.
        /// Returned options value represents about the transaction mets the minimal precondition or not.
        /// </summary>
        /// <returns></returns>
        public TransactionPackingOptions GetPreconditionStatus()
        {
            return new TransactionPackingOptions
            {
                WithId = Header.TrxId != HashValue.Empty,
                WithSenderSeal = Header.SenderSeal != SignValue.Empty,
                WithWitness = Witness.Accounts.Count > 0 && Witness.AccountSeals.Count > 0
            };
        }

        /// <summary>
        /// Transaction Status. (Not Serialized)
        /// </summary>
        public TransactionStatus Status { get; set; }

        /// <summary>
        /// Transaction Behaviours.
        /// </summary>
        public List<TransactionBehaviour> Behaviours
        {
            get => Ensures(ref m_Behaviours);
            set => Assigns(ref m_Behaviours, value);
        }

        /// <summary>
        /// Expected Results.
        /// Pairs that points a slot.
        /// Empty value is about removal of the slot, otherwise alter(or create).
        /// </summary>
        public Dictionary<HistoryColumnKey, byte[]> ExpectedRecords
        {
            get => Ensures(ref m_ExpectedRecords);
            set => Assigns(ref m_ExpectedRecords, value);
        }
    }
}
