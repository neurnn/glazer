using Backrole.Crypto;
using System.Collections.Generic;
using static Glazer.Nodes.Helpers.ModelHelpers;

namespace Glazer.Nodes.Models.Transactions
{
    public class Transaction
    {
        private TransactionHeader m_Header;
        private TransactionWitness m_Witness;
        private List<TransactionBehaviour> m_Behaviours;

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
    }
}
