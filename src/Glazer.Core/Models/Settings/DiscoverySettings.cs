using Backrole.Core.Abstractions;
using System.Net;

namespace Glazer.Core.Models.Settings
{
    public class DiscoverySettings
    {
        /// <summary>
        /// Max TTL of the discovery messages.
        /// </summary>
        public int MaxTtl { get; set; } = 16;

        /// <summary>
        /// Address that is to be advertised.
        /// Null to use the requester's seeing value.
        /// </summary>
        public IPAddress AdsAddress { get; set; } = null;

        /// <summary>
        /// Port that is to be advertised.
        /// </summary>
        public int AdsPort { get; set; } = 7000;

        /// <summary>
        /// Indicates whether the discovery uses requester-seeing address as ads address.
        /// </summary>
        public bool UseRequesterSeeing => AdsAddress is null;

        /// <summary>
        /// Fill the settings from <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="Configs"></param>
        /// <returns></returns>
        public DiscoverySettings From(IConfiguration Configs)
        {
            MaxTtl = int.Parse(Configs["discovery:max_ttl"] ?? "16");
            AdsAddress = null; AdsPort = 7000;

            if (!string.IsNullOrWhiteSpace(Configs["discovery:intro:address"]))
            {
                AdsAddress = IPAddress.Parse(Configs["discovery:ads:address"]);
                AdsPort = int.Parse(Configs["discovery:ads:port"] ?? "7000");
            }
            return this;
        }
    }
}
