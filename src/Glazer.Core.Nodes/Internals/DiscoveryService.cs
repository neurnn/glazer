using Backrole.Core.Abstractions;
using Backrole.Core.Hosting;
using Glazer.Core.Nodes.Internals.Messages;
using Glazer.Core.Nodes.Internals.Remotes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals
{
    /// <summary>
    /// Transmits the <see cref="Heartbeat"/> messages to remote nodes
    /// And push the received endpoints to the <see cref="NodeNetwork"/> instance.
    /// </summary>
    internal class DiscoveryService : BackgroundService
    {
        private NodeNetwork m_Network;
        private NodeNetworkSettings m_Settings;

        private ILogger m_Logger;

        /// <summary>
        /// Initialize a new <see cref="DiscoveryService"/>
        /// </summary>
        /// <param name="Network"></param>
        public DiscoveryService(NodeNetwork Network, IOptions<NodeNetworkSettings> Options, ILogger<DiscoveryService> Logger = null)
        {
            m_Network = Network;
            m_Settings = Options.Value;
            m_Logger = Logger;

            /* Register the request handler. */
            Network.ListenRequest(OnRequest);
        }

        /// <summary>
        /// Run the remote node discovery.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected override async Task RunAsync(CancellationToken Token)
        {
            foreach (var Seed in m_Settings.Seeds)
                m_Network.Push(Seed);

            if (m_Settings.AdvertiseEndpoint is null)
                return;

            var Now = DateTime.Now;
            while (!Token.IsCancellationRequested)
            {
                var Elapsed = DateTime.Now - Now;
                if (Elapsed.TotalSeconds < m_Settings.DiscoveryInterval)
                {
                    var Sleep = TimeSpan.FromSeconds(m_Settings.DiscoveryInterval - Elapsed.TotalSeconds);
                    try { await Task.Delay(Sleep, Token); } catch { }

                    Now = DateTime.Now;
                    continue;
                }

                var Advertise = new Heartbeat
                {
                    Ttl = m_Settings.DiscoveryTtl,
                    Endpoint = m_Settings.AdvertiseEndpoint
                };

                var Sent = 0;
                var Replies = await m_Network.InvokeAsync(Node =>
                {
                    Sent++;
                    return Node
                        .GetService<RemoteNodeSender>()
                        .Request(Advertise, Token);
                });

                var Nodes = Replies
                    .Where(X => X is HeartbeatReply)
                    .Select(X => X as HeartbeatReply)
                    .Sum(X => X.DeliveredNodes);

                if (Nodes > 0)
                    m_Logger?.Info($"{Nodes} nodes received the discovery advertisement.");

                else if (Sent <= 0)
                {
                    m_Logger?.Info("No remote nodes are connected. retry to connect to initial seeds.");

                    /* Re-connect to initial seeds. */
                    foreach (var Seed in m_Settings.Seeds)
                        m_Network.Push(Seed);
                }

                Now = DateTime.Now;
            }
        }

        /// <summary>
        /// Called when the request instance arrived.
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        private async Task<object> OnRequest(INode Node, object Message)
        {
            if (Message is not Heartbeat Heartbeat)
                return null;

            int Nodes = 0;

            if (--Heartbeat.Ttl > 0) /* Propagate the advertisement to remote nodes. */
            {
                var Token = Node.GetRequiredService<CancellationToken>();
                var Replies = await m_Network.InvokeAsync(Node =>
                {
                    return Node
                        .GetService<RemoteNodeSender>()
                        .Request(Heartbeat, Token);
                });

                Nodes = Replies
                    .Where(X => X is HeartbeatReply)
                    .Select(X => X as HeartbeatReply)
                    .Sum(X => X.DeliveredNodes);
            }

            /* Add a remote node if it isn't self advertisement. */
            if (Heartbeat.Endpoint != m_Settings.AdvertiseEndpoint)
            {
                Nodes++;
                m_Network.Push(Heartbeat.Endpoint);
            }

            return new HeartbeatReply
            {
                DeliveredNodes = Nodes
            };
        }

    }
}
