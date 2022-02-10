using Backrole.Core.Abstractions;
using Glazer.Core.Helpers;
using System;
using System.Net;

namespace Glazer.Core.Models.Settings
{
    public class HttpSettings
    {
        /// <summary>
        /// Address to listen.
        /// </summary>
        public IPAddress Address { get; set; } = IPAddress.Any;

        /// <summary>
        /// Port number to listen.
        /// </summary>
        public int Port { get; set; } = 8080;

        /// <summary>
        /// Enable Cors headers.
        /// </summary>
        public bool EnableCors { get; set; } = false;

        /// <summary>
        /// Fill the settings from <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="Configs"></param>
        /// <returns></returns>
        public HttpSettings From(IConfiguration Configs)
        {
            Address = IPAddress.Parse(Configs["http:address"] ?? "0.0.0.0");
            Port = int.Parse(Configs["http:port"] ?? "8080");

            EnableCors = (Configs["http:cors"] ?? "disabled").CaseEquals("enabled");
            return this;
        }
    }
}
