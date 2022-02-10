using Backrole.Crypto;
using System;
using System.Collections.Generic;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Models.Codes
{
    public class Code
    {
        private List<string> m_Names;

        /// <summary>
        /// Code Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Hash Value.
        /// </summary>
        public HashValue Hash { get; set; }

        /// <summary>
        /// Time Stamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Type of the code.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Function Names.
        /// </summary>
        public List<string> Names
        {
            get => Ensures(ref m_Names);
            set => Assigns(ref m_Names, value);
        }
    }
}
