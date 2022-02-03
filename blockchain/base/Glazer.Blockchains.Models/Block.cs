﻿using Glazer.Core.Cryptography;
using System.Collections.Generic;

namespace Glazer.Blockchains.Models
{
    public sealed class Block
    {
        private BlockHeader m_Header;
        
        /// <summary>
        /// Header of the block.
        /// </summary>
        public BlockHeader Header
        {
            get => m_Header;
            set
            {
                if ((m_Header = value) != null)
                     m_Header.Block = this;
            }
        }

        /// <summary>
        /// Transaction records.
        /// </summary>
        public List<Transaction> Transactions { get; set; }

        /// <summary>
        /// Seal of the block that is generated by the block producer.
        /// </summary>
        public Seal Seal { get; set; }
    }
}