using Backrole.Crypto;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Glazer.Nodes.Helpers.ModelHelpers;


namespace Glazer.Nodes.Models.Networks
{
    /// <summary>
    /// Request from the remote hosts.
    /// </summary>
    public class NodeRequest
    {
        private Dictionary<string, string> m_Headers;
        private Dictionary<string, string> m_Options;
        private Dictionary<string, byte[]> m_Blobs;

        private DateTime? m_Expiration;
        private ReplyHandler m_Reply;
        private int m_Replied;

        /// <summary>
        /// Delegate that invoked by <see cref="ReplyAsync(NodeResponse)"/> method.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Response"></param>
        /// <returns></returns>
        public delegate Task ReplyHandler(NodeRequest Request, NodeResponse Response);

        /// <summary>
        /// Initialize a new <see cref="NodeRequest"/> instance.
        /// </summary>
        /// <param name="Reply"></param>
        public NodeRequest(ReplyHandler Reply, CancellationToken Aborted)
        {
            this.Aborted = Aborted;

            m_Replied = 0;
            m_Reply = Reply;
        }

        /// <summary>
        /// Node that accepted this request.
        /// </summary>
        public Node Node { get; set; }

        /// <summary>
        /// Expiration Time of the request.
        /// </summary>
        public DateTime Expiration
        {
            get => Initiate(ref m_Expiration, () => DateTime.UtcNow.AddSeconds(30));
            set
            {
                if (value.Kind != DateTimeKind.Utc)
                    m_Expiration = value.ToUniversalTime();

                else
                    m_Expiration = value;
            }
        }

        /// <summary>
        /// Triggered when the request is aborted.
        /// </summary>
        public CancellationToken Aborted { get; }

        /// <summary>
        /// Test whether the request handled or not.
        /// </summary>
        public bool IsReplied => m_Replied != 0;

        /// <summary>
        /// Properties that shared between the request handlers.
        /// </summary>
        public Dictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Request Headers.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get => Ensures(ref m_Headers);
            set => Assigns(ref m_Headers, value);
        }

        /// <summary>
        /// Path to invoke.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Method of the request.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Query parameters.
        /// </summary>
        public Dictionary<string, string> Options
        {
            get => Ensures(ref m_Options);
            set => Assigns(ref m_Options, value);
        }

        /// <summary>
        /// Sender that set on the request itself.
        /// </summary>
        public Account Sender { get; set; }

        /// <summary>
        /// Sender's signature.
        /// </summary>
        public SignValue SenderSign { get; set; }

        /// <summary>
        /// Data Blobs that sent with request body.
        /// </summary>
        public Dictionary<string, byte[]> Blobs
        {
            get => Ensures(ref m_Blobs);
            set => Assigns(ref m_Blobs, value);
        }

        /// <summary>
        /// Request Body instance.
        /// </summary>
        public JObject Body { get; set; }

        /// <summary>
        /// Reply the response to the remote host.
        /// </summary>
        /// <param name="Response"></param>
        public Task ReplyAsync(NodeResponse Response)
        {
            ReplyHandler Reply;

            if (Interlocked.CompareExchange(ref m_Replied, 1, 0) != 0)
                return Task.CompletedTask;

            if ((Reply = m_Reply) is null)
                return Task.CompletedTask;

            return Reply.Invoke(this, Response);
        }
    }
}
