using Newtonsoft.Json.Linq;

namespace Glazer.Nodes.Models.Interfaces
{
    /// <summary>
    /// Json Message interface.
    /// </summary>
    public interface IJsonMessage
    {
        /// <summary>
        /// Encode the message to <see cref="JObject"/>.
        /// </summary>
        /// <returns></returns>
        void Encode(JObject Json);

        /// <summary>
        /// Decode the message from <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        void Decode(JObject Json);
    }
}
