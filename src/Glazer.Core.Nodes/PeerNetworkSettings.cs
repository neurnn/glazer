using Backrole.Core.Abstractions;
using System;
using System.Net;

namespace Glazer.Core.Nodes
{
    public class PeerNetworkSettings
    {
        /// <summary>
        /// Local Endpoint of the peer network.
        /// </summary>
        public IPEndPoint LocalEndpoint { get; set; } = new IPEndPoint(IPAddress.Any, 5000);

        /// <summary>
        /// Connection Timeout.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Recovery Timeout.
        /// </summary>
        public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Tick interval in ms.
        /// </summary>
        public int TickInterval { get; set; } = 150;

        /// <summary>
        /// Load configurations from <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="Configs"></param>
        /// <returns></returns>
        public PeerNetworkSettings From(IConfiguration Configs)
        {
            var Address = IPAddress.Parse(Configs["p2p:endpoint:address"] ?? "0.0.0.0");
            var Port = int.Parse(Configs["p2p:endpoint:port"] ?? "7000");

            var ConnTO = double.Parse(Configs["p2p:timeouts:connect" ?? "5"]);
            var RecvTO = double.Parse(Configs["p2p:timeouts:recovery"] ?? "30");

            var Tick = double.Parse(Configs["p2p:tick"] ?? "0.15");

            LocalEndpoint = new IPEndPoint(Address, Port);
            ConnectionTimeout = TimeSpan.FromSeconds(ConnTO);
            RecoveryTimeout = TimeSpan.FromSeconds(RecvTO);

            TickInterval = (int)(1000 * Math.Max(Tick, 0.05));
            return this;
        }
    }
}
