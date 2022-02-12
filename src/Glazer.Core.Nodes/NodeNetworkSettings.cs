using Backrole.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using static Glazer.Core.Helpers.ModelHelpers;

namespace Glazer.Core.Nodes
{
    public class NodeNetworkSettings
    {
        private List<IPEndPoint> m_Seeds;

        /// <summary>
        /// Local Endpoint of the peer network.
        /// </summary>
        public IPEndPoint LocalEndpoint { get; set; } = new IPEndPoint(IPAddress.Any, 7000);

        /// <summary>
        /// Advertisement Endpoint of the peer network.
        /// </summary>
        public IPEndPoint AdvertiseEndpoint { get; set; }

        /// <summary>
        /// Seeds of the peer network.
        /// </summary>
        public List<IPEndPoint> Seeds
        {
            get => Ensures(ref m_Seeds);
            set => m_Seeds = value;
        }

        /// <summary>
        /// Discovery Interval in seconds.
        /// </summary>
        public int DiscoveryInterval { get; set; } = 15;

        /// <summary>
        /// Discovery TTL.
        /// </summary>
        public int DiscoveryTtl { get; set; } = 16;

        /// <summary>
        /// Load configurations from <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="Configs"></param>
        /// <returns></returns>
        public NodeNetworkSettings From(IConfiguration Configs)
        {
            var Tick = double.Parse(Configs["p2p:discovery:interval"] ?? "15");

            LocalEndpoint = IPEndPoint.Parse(Configs["p2p:endpoint"]);
            DiscoveryInterval = (int)(1000 * Math.Max(Tick, 0.05));

            var Peers = Configs.Keys.Where(X => X.StartsWith("p2p:peers:"));
            foreach(var Key in Peers)
                Seeds.Add(IPEndPoint.Parse(Configs[Key]));

            if (Configs["p2p:discovery:advertise"] != null)
                AdvertiseEndpoint = IPEndPoint.Parse(Configs["p2p:discovery:advertise"]);

            DiscoveryTtl = int.Parse(Configs["p2p:discovery:ttl"] ?? "16");
            return this;
        }
    }
}
