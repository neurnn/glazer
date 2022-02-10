using Backrole.Crypto;
using System.Collections.Generic;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Blocks
{
    /// <summary>
    /// Witness datas of the block.
    /// </summary>
    public class BlockWitness
    {
        private List<Account> m_Accounts;
        private List<SignValue> m_AccountSeals;

        /// <summary>
        /// Block Model.
        /// This will be set when the header instance assigned on the transaction.
        /// </summary>
        public Block Block { get; internal set; }

        /// <summary>
        /// Accounts that verified this block.
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
