using Glazer.P2P.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeEngine
    {
        /// <summary>
        /// Run the <see cref="INodeEngine"/> instance.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken Token = default);
    }
}
