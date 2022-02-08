using Glazer.Nodes.Exceptions;
using Glazer.Nodes.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Models.Contracts
{
    /// <summary>
    /// Node Feature.
    /// </summary>
    public abstract class NodeFeature
    {
        private NodeStatus m_Status = NodeStatus.Created;
        private List<Func<NodeStatus, Task>> m_StatusChanged = new();
        private List<Func<NodeRequest, Task<NodeResponse>>> m_Subscribers = new();

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
        /// Type of the node.
        /// </summary>
        public abstract NodeFeatureType NodeType { get; }

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
        public virtual async Task<bool> WaitAsync(NodeStatus Status, CancellationToken Token = default)
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
        /// Subscribe requests from the node.
        /// </summary>
        /// <param name="Subscriber"></param>
        /// <returns></returns>
        public virtual IDisposable SubscribeRequests(Func<NodeRequest, Task<NodeResponse>> Subscriber)
        {
            if (Subscriber is null)
                throw new ArgumentNullException(nameof(Subscriber));

            lock (m_Subscribers)
            {
                m_Subscribers.Add(Subscriber);

                if (m_Subscribers.Count == 1)
                    OnBeginSubscription();
            }

            return new Unsubscribe
            {
                Action = () =>
                {
                    lock (m_Subscribers)
                    {
                        m_Subscribers.Remove(Subscriber);

                        if (m_Subscribers.Count <= 0)
                            OnEndSubscription();
                    }
                }
            };
        }

        /// <summary>
        /// Deliver the request to subscribers.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        protected virtual async Task<NodeResponse> DeliverAsync(NodeRequest Request)
        {
            var Queue = m_Subscribers.Locked(X => new Queue<Func<NodeRequest, Task<NodeResponse>>>(X));

            while (Queue.TryDequeue(out var Subscriber))
            {
                var Response = await Subscriber(Request);
                if (Response != null && Response.Message != null)
                    return Response;
            }

            return null;
        }

        /// <summary>
        /// Called when the first subscriber starts their subscription.
        /// </summary>
        protected virtual void OnBeginSubscription()
        {

        }

        /// <summary>
        /// Called when the last subscriber ends their subscription.
        /// </summary>
        protected virtual void OnEndSubscription()
        {

        }

        /// <summary>
        /// Unsubscribe on dispose.
        /// </summary>
        private struct Unsubscribe : IDisposable
        {
            public Action Action;
            public void Dispose()
            {
                Action?.Invoke();
                Action = null;
            }
        }
    }
}
