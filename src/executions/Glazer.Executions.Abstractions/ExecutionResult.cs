using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Memory;
using Newtonsoft.Json;

namespace Glazer.Executions.Abstractions
{
    public struct ExecutionResult
    {
        /// <summary>
        /// Indicates whether the execution was successful or not.
        /// </summary>
        [JsonProperty("succeed")]
        public bool Succeed { get; set; }

        /// <summary>
        /// Reason if the execution was failed.
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// Output KV Table.
        /// </summary>
        [JsonIgnore]
        public MemoryKvReadOnlyTable Outputs { get; set; }
    }
}
