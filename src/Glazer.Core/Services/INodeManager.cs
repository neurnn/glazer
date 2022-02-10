using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Services
{
    /// <summary>
    /// Node Manager instance.
    /// These are exists only for the local nodes.
    /// </summary>
    public interface INodeManager
    {
        /// <summary>
        /// Add a node instance to the manager.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        bool Add(INode Node);

        /// <summary>
        /// Remove a node instance from the manager.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        bool Remove(INode Node);

        /// <summary>
        /// Find a node instance from the manager.
        /// </summary>
        /// <param name="Selector"></param>
        /// <returns></returns>
        INode Find(Predicate<INode> Selector);

        /// <summary>
        /// Find a last node instance from the manager.
        /// </summary>
        /// <param name="Selector"></param>
        /// <returns></returns>
        INode FindLast(Predicate<INode> Selector);

        /// <summary>
        /// Find all node instances from the manager.
        /// </summary>
        /// <param name="Collector"></param>
        /// <param name="Selector"></param>
        /// <returns></returns>
        int FindAll(IList<INode> Collector, Predicate<INode> Selector);

        /// <summary>
        /// Waits for peers until the specified condition is satisfied asynchronously.<br />
        /// <b>Note</b>: Even if the condition is met, if there are no new nodes, it will wait new nodes infinitely.
        /// </summary>
        /// <param name="Condition"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task WaitAsync(Func<bool> Condition, CancellationToken Token = default);
    }
}
