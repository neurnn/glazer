using Glazer.Nodes.Abstractions;
using Glazer.P2P.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Internals
{
    public class MultiEngine : INodeEngine
    {
        private IServiceProvider m_Services;

        /// <summary>
        /// Initialize a new <see cref="MultiEngine"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        public MultiEngine(IServiceProvider Services) => m_Services = Services;

        /// <summary>
        /// Run the multi engine instance.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken Token = default)
        {
            var Sync = m_Services
                .GetRequiredService<INodeSynchronizationManager>();

            Sync.SetEnabled(true);
            try
            {

            }
            finally
            {
                Sync.SetEnabled(false);
            }
        }
    }
}
