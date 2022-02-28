using Backrole.Crypto;
using Glazer.Common.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Glazer.Accessors
{
    public struct AccountAbi
    {
        /// <summary>
        /// Initialize a new <see cref="AccountAbi"/> instance.
        /// </summary>
        /// <param name="Actor"></param>
        public AccountAbi(Actor Actor)
        {
            this.Actor = Actor;

            PublicKeys = new List<SignPublicKey>();
        }

        /// <summary>
        /// Actor information.
        /// </summary>
        [JsonIgnore]
        public Actor Actor { get; private set; }

        /// <summary>
        /// Public Keys to authenticate.
        /// </summary>
        [JsonProperty("pub_keys")]
        public List<SignPublicKey> PublicKeys { get; set; }
    }
}
