using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Glazer.Storages.Internals.Http
{
    public struct LockRequest
    {
        /// <summary>
        /// Key to lock.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Expiration in seconds.
        /// </summary>
        [JsonProperty("expiration")]
        public double Expiration { get; set; }

        /// <summary>
        /// Make the lock request content.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public static StringContent Make(LockRequest Request)
        {
            return new StringContent(
                JsonConvert.SerializeObject(Request),
                Encoding.UTF8, "application/json");
        }
    }
}
