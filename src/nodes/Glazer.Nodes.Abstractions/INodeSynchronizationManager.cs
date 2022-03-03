using Glazer.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeSynchronizationManager
    {
        /// <summary>
        /// Indicates whether the synchronization is enabled or not.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Set the enabled value to be specified <paramref name="Value"/>.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        INodeSynchronizationManager SetEnabled(bool Value);

        /// <summary>
        /// Request a block synchronization manually.
        /// </summary>
        /// <param name="BlockId"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> RequestAsync(BlockId BlockId, CancellationToken Token = default);
    }
}
