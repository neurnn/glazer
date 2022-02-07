using Glazer.Nodes.Models.Transactions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Glazer.Nodes.Helpers.ModelHelpers;

namespace Glazer.Nodes.Models.Blocks
{
    public class Block
    {
        private BlockHeader m_Header;
        private BlockWitness m_Witness;
        private List<Transaction> m_Transactions;

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
        /// Block Witness.
        /// </summary>
        public BlockWitness Witness
        {
            get => Ensures(ref m_Witness, X => X.Block = this);
            set => Assigns(ref m_Witness, value).Block = this;
        }
    }
}
