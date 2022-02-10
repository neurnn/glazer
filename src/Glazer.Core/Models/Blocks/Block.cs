using Glazer.Core.Models.Transactions;
using Glazer.Core.Records;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Blocks
{
    public class Block
    {
        private BlockHeader m_Header;
        private BlockWitness m_Witness;
        private List<Transaction> m_Transactions;
        private Dictionary<HistoryColumnKey, byte[]> m_Records;

        /// <summary>
        /// Block Header.
        /// </summary>
        public BlockHeader Header
        {
            get => Ensures(ref m_Header, X => X.Block = this);
            set => Assigns(ref m_Header, value).Block = this;
        }

        /// <summary>
        /// Transactions that recorded here.
        /// </summary>
        public List<Transaction> Transactions
        {
            get => Ensures(ref m_Transactions);
            set => Assigns(ref m_Transactions, value);
        }

        /// <summary>
        /// Hardened Records.
        /// </summary>
        public Dictionary<HistoryColumnKey, byte[]> Records
        {
            get => Ensures(ref m_Records);
            set => Assigns(ref m_Records, value);
        }

        /// <summary>
        /// Block Witness.
        /// </summary>
        public BlockWitness Witness
        {
            get => Ensures(ref m_Witness, X => X.Block = this);
            set => Assigns(ref m_Witness, value).Block = this;
        }
    }
}
