using Glazer.P2P.Hosting;
using Glazer.P2P.Hosting.Impls;
using Glazer.P2P.Integration.AspNetCore.Internals;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Glazer.P2P.Integration.AspNetCore
{
    public static class Extensions
    {
        /// <summary>
        /// Add the <see cref="IMessangerHost"/> to the ASP.NET core.
        /// </summary>
        /// <param name="Services"></param>
        /// <returns></returns>
        public static IMessangerHostBuilder AddP2PHostService(this IServiceCollection Services)
        {
            var HostBuilder = new MessangerHostBuilder();

            Services
                .AddSingleton(_ => HostBuilder.Build())
                .AddHostedService<P2PHostService>();

            return HostBuilder;
        }
    }
}
