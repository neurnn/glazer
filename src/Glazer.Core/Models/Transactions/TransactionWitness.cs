using Backrole.Crypto;
using System.Collections.Generic;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Transactions
{
    /// <summary>
    /// Witness datas of the transaction.
    /// </summary>
    public class TransactionWitness
    {
        private List<Account> m_Accounts;
        private List<SignValue> m_AccountSeals;

        /// <summary>
        /// Transaction Model.
        /// This will be set when the header instance assigned on the transaction.
        /// </summary>
        public Transaction Transaction { get; internal set; }

        /// <summary>
        /// Accounts that verified this transaction.
        /// </summary>
        public List<Account> Accounts
        {
            get => Ensures(ref m_Accounts);
            set => Assigns(ref m_Accounts, value);
        }

        /// <summary>
        /// Account Seals to verify accounts who listed on <see cref="Accounts"/>.
        /// </summary>
        public List<SignValue> AccountSeals
        {
            get => Ensures(ref m_AccountSeals);
            set => Assigns(ref m_AccountSeals, value);
        }
    }
}
