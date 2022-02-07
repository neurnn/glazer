using Glazer.Nodes.Models;
using Glazer.Nodes.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Glazer.Nodes.Helpers;

namespace Glazer.Nodes.Models.Networks
{
    /// <summary>
    /// Node interface.
    /// </summary>
    public abstract class Node : IAsyncDisposable
    {
        private NodeStatus m_Status = NodeStatus.Created;
        private List<Func<NodeStatus, Task>> m_StatusChanged = new();

        /// <summary>
        /// Test whether the node is from remote or not.
        /// </summary>
        public abstract bool IsRemote { get; }

        /// <summary>
        /// Test whether the node is remote initiated or not.
        /// In other words, the node is connected by the remote host or not.
        /// More simply, if false, the local computer has connected to the remote host.<br />
        /// <code>
        /// true: remote initiated connection (server-mode connection)<br />
        /// false: local initiated connection (client-mode connection)
        /// </code>
        /// </summary>
        public abstract bool IsRemoteInitiated { get; }

        /// <summary>
        /// Features that supported by the node.
        /// </summary>
        public abstract NodeFeature[] Features { get; }

        /// <summary>
        /// Status of the node.
        /// </summary>
        public NodeStatus Status => this.LockedGet(ref m_Status);

        /// <summary>
        /// Node account to identifying the node. (not request account)
        /// </summary>
        public abstract Account Account { get; }

        /// <summary>
        /// Event that notifies changes of the status.
        /// </summary>
        public event Func<NodeStatus, Task> StatusChanged
        {
            add => m_StatusChanged.AddLocked(value);
            remove => m_StatusChanged.RemoveLocked(value);
        }

        /// <summary>
        /// Set <see cref="Status"/> to the specified value,
        /// And notify its changes to handlers.
        /// </summary>
        /// <param name="Status"></param>
        protected void SetStatus(NodeStatus Status)
        {
            lock (this)
            {
                if (m_Status == Status)
                    return;

                m_Status = Status;
            }

            NotifyStatus(Status, m_StatusChanged.ToQueueLocked());
        }

        /// <summary>
        /// Notify the status changes to handlers.
        /// </summary>
        /// <param name="Status"></param>
        /// <param name="Queue"></param>
        private static void NotifyStatus(NodeStatus Status, Queue<Func<NodeStatus, Task>> Queue)
        {
            try
            {
                while (Queue.TryDequeue(out var Handler))
                    Handler(Status).GetAwaiter().GetResult();
            }

            finally
            {
                if (Queue.Count > 0)
                    NotifyStatus(Status, Queue);
            }
        }

        /// <summary>
        /// Waits for this node instance to reach a certain state.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> WaitAsync(NodeStatus Status, CancellationToken Token = default)
        {
            if (this.LockedGet(ref m_Status) != Status)
            {
                var Tcs = new TaskCompletionSource<bool>();
                Task OnStatusChanged(NodeStatus NewStatus)
                {
                    if (NewStatus == Status)
                        Tcs.TrySetResult(true);

                    return Task.CompletedTask;
                }

                StatusChanged += OnStatusChanged;
                try
                {
                    using (Token.Register(() => Tcs.TrySetResult(false)))
                        return await Tcs.Task;
                }

                finally { StatusChanged -= OnStatusChanged; }
            }

            return true;
        }

        /// <summary>
        /// Execute the <see cref="NodeRequest"/> on this node asynchronously.
        /// </summary>
        /// <exception cref="NodeStatusException">Node status unmet.</exception>
        /// <exception cref="NodeConnectivityException">Node connection dead until sending request.</exception>
        /// <exception cref="TimeoutException">Expiration reached before completion of the request.</exception>
        /// <param name="Request"></param>
        /// <returns></returns>
        public abstract Task<NodeResponse> ExecuteAsync(NodeRequest Request);

        /// <summary>
        /// Disposes the internal connection instance and discard all states.
        /// </summary>
        /// <returns></returns>
        public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
