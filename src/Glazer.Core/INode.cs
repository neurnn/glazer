using Glazer.Core.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core
{
    /// <summary>
    /// Node interface.
    /// </summary>
    public interface INode : IServiceProvider
    {
        /// <summary>
        /// Status of the node.
        /// </summary>
        NodeStatus Status { get; }

        /// <summary>
        /// Event that notifies the node status changed.
        /// </summary>
        event Action<INode> StatusChanged;

        /// <summary>
        /// Remote Endpoint that points the target node.
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Account of the node.
        /// </summary>
        Account Account { get; }
    }
}
