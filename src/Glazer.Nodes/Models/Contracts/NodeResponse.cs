using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using static Glazer.Nodes.Helpers.ModelHelpers;


namespace Glazer.Nodes.Models.Contracts
{
    /// <summary>
    /// Response from the remote hosts.
    /// </summary>
    public class NodeResponse
    {
        private Dictionary<string, string> m_Headers;

        /// <summary>
        /// Request Headers.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get => Ensures(ref m_Headers);
            set => Assigns(ref m_Headers, value);
        }

        /// <summary>
        /// Response Message instance.
        /// </summary>
        public object Message { get; set; }
    }
}
