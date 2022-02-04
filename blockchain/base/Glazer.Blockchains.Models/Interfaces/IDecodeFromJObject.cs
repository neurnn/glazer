using Newtonsoft.Json.Linq;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IDecodeFromJObject
    {
        /// <summary>
        /// Decode from <see cref="JObject"/>
        /// </summary>
        /// <param name="JObject"></param>
        void DecodeFromJObject(JObject JObject, NodeOptions Options);
    }
}
