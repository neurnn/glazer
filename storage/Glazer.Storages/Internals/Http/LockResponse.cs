using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Glazer.Storages.Internals.Http
{
    public struct LockResponse
    {
        /// <summary>
        /// Token that used to unlock the key.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Locked Time.
        /// </summary>
        [JsonProperty("locked_time")]
        public double LockedTime { get; set; }

        /// <summary>
        /// Expiration.
        /// </summary>
        [JsonProperty("expiration")]
        public double Expiration { get; set; }

        /// <summary>
        /// Make the LockResponse from the <see cref="HttpContent"/> instance.
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public static async Task<LockResponse> MakeAsync(HttpContent Content)
        {
            var Json = await Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<LockResponse>(Json);
        }
    }
}
