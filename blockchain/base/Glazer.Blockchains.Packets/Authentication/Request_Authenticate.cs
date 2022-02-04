using Glazer.Core.Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Packets.Authentication
{
    public class Request_Authenticate
    {
        /// <summary>
        /// Phrase to sign message by the peer's private key.
        /// </summary>
        [JsonProperty("phrase_to_sign")]
        public string PhraseToSign { get; set; }
    }

    public class Response_Authenticate
    {
        [JsonProperty("phrase_signed")]
        public string PhraseSigned { get; set; }
    }
}
