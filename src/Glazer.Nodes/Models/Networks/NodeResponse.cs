using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using static Glazer.Nodes.Helpers.ModelHelpers;


namespace Glazer.Nodes.Models.Networks
{
    /// <summary>
    /// Response from the remote hosts.
    /// </summary>
    public class NodeResponse
    {
        private Dictionary<string, string> m_Headers;
        private Dictionary<string, byte[]> m_Blobs;

        /// <summary>
        /// Response Status. this uses same system with Http.
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.NotFound;

        /// <summary>
        /// Request Headers.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get => Ensures(ref m_Headers);
            set => Assigns(ref m_Headers, value);
        }

        /// <summary>
        /// Data Blob contents.
        /// </summary>
        public Dictionary<string, byte[]> Blobs
        {
            get => Ensures(ref m_Blobs);
            set => Assigns(ref m_Blobs, value);
        }

        /// <summary>
        /// Response Body instance.
        /// </summary>
        public JObject Body { get; set; }
    }
}
