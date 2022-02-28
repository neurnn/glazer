using Glazer.P2P.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.P2P.Integration.AspNetCore.Internals
{
    internal class P2PHostService : IHostedService
    {
        private IMessangerHost m_Host;

        /// <summary>
        /// Initialize a new <see cref="P2PHostService"/> instance.
        /// </summary>
        /// <param name="Host"></param>
        public P2PHostService(IMessangerHost Host) 
            => m_Host = Host;


        public Task StartAsync(CancellationToken Token) => m_Host.StartAsync(Token);
        public Task StopAsync(CancellationToken Token) => m_Host.StopAsync();
    }
}
