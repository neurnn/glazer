using Glazer.Core.Cryptography;
using Newtonsoft.Json.Linq;
using System;

namespace Glazer.Blockchains.Models
{
    public struct Record
    {
        /// <summary>
        /// ETag of the record.
        /// </summary>
        public Guid? Etag { get; set; }

        /// <summary>
        /// Value of the record.
        /// </summary>
        public JObject Value { get; set; }

        /// <summary>
        /// Transaction Reference.
        /// </summary>
        public TransactionRef TrxRef { get; set; }
    }
}
