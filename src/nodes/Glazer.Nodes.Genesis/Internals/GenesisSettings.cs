using Glazer.Common.Common;
using Glazer.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Glazer.Nodes.Internals
{
    internal class GenesisSettings
    {
        /// <summary>
        /// Initial Block Id.
        /// </summary>
        [JsonProperty("initial_block_id")]
        public string InitialBlockId { get; set; }

        /// <summary>
        /// Initial KV Datas.
        /// </summary>
        [JsonProperty("initial_kv_datas")]
        public Dictionary<string, string> InitialKvData { get; set; } = new();

        /// <summary>
        /// Initial Time Stamp.
        /// </summary>
        [JsonProperty("initial_timestamp")]
        public string InitialTimeStamp { get; set; }

        /// <summary>
        /// Get the initial block id.
        /// </summary>
        /// <returns></returns>
        public BlockId GetInitialBlockId()
        {
            return new BlockId(Guid.Parse(InitialBlockId));
        }

        /// <summary>
        /// Get the initial time-stamp.
        /// </summary>
        /// <returns></returns>
        public TimeStamp GetInitialTimeStamp()
        {
            return DateTimeHelpers.ParseRfc1123(InitialTimeStamp);
        }
    }
}
