using Newtonsoft.Json.Linq;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IEncodeToJObject
    {
        /// <summary>
        /// Encode to <see cref="JObject"/>.
        /// </summary>
        /// <returns></returns>
        JObject EncodeToJObject(NodeOptions Options);
    }
}
