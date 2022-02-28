using Glazer.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Glazer.Accessors
{
    public struct ProtocolAbi
    {
        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        /// <summary>
        /// Indicates whether the protocol abi is valid or not.
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Owner) &&
            !string.IsNullOrWhiteSpace(Type) &&
            !string.IsNullOrWhiteSpace(Data);
    }
}
