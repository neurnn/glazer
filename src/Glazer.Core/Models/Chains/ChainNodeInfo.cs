using Backrole.Core.Abstractions;
using Backrole.Crypto;
using System;
using System.Globalization;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Chains
{
    public class ChainNodeInfo
    {
        private ChainInfo m_ChainInfo;

        /// <summary>
        /// Time Stamp when this node encoded this.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Chain Information.
        /// </summary>
        public ChainInfo Chain
        {
            get => Ensures(ref m_ChainInfo);
            set => m_ChainInfo = value;
        }

        /// <summary>
        /// Account that this node introduces.
        /// </summary>
        public Account Account { get; set; }

        /// <summary>
        /// Signature.
        /// </summary>
        public SignValue Signature { get; set; }
    }
}
