using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace Glazer.Storages.Internals.Http
{
    public struct UnlockRequest
    {
        /// <summary>
        /// Key to unlock.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Token that used to unlock the key.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Make the unlock request content.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public static StringContent Make(UnlockRequest Request)
        {
            return new StringContent(
                JsonConvert.SerializeObject(Request),
                Encoding.UTF8, "application/json");
        }
    }
}
