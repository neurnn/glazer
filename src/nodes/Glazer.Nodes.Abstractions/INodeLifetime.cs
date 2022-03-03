using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Glazer.Nodes.Abstractions
{
    public interface INodeLifetime
    {
        /// <summary>
        /// Triggered when the node application is started.
        /// </summary>
        CancellationToken Starting { get; }

        /// <summary>
        /// Triggered when the node application is started.
        /// </summary>
        CancellationToken Started { get; }

        /// <summary>
        /// Triggered when the node application is stopping.
        /// </summary>
        CancellationToken Stopping { get; }

        /// <summary>
        /// Triggered when the node application is stopped.
        /// </summary>
        CancellationToken Stopped { get; }
    }
}
