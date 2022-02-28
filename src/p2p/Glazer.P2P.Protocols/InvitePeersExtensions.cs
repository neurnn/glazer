using Backrole.Crypto;
using Glazer.P2P.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.P2P.Protocols
{
    public static class InvitePeersExtensions
    {
        /// <summary>
        /// Invites other peers on the P2P network.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Expiration"></param>
        public static void InvitePeers(this IMessanger Messanger, long Expiration = 5)
        {
            Protocols.InvitePeers.Emit(Messanger, Expiration);
        }

        /// <summary>
        /// Invites the specific receiver on the P2P network.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Expiration"></param>
        public static void InvitePeer(this IMessanger Messanger, SignPublicKey Receiver, long Expiration = 5)
        {
            Protocols.InvitePeers.Emit(Messanger, Receiver, Expiration);
        }

        /// <summary>
        /// Invites other peers on the P2P network and wait their connection.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Expiration"></param>
        /// <returns></returns>
        public static async Task<SignPublicKey[]> InviteAndWaitAsync(this IMessanger Messanger, long Expiration = 5, CancellationToken Token = default)
        {
            var Peers = new HashSet<SignPublicKey>();

            void OnPeerEnter(SignPublicKey Key)
            {
                lock (Peers)
                      Peers.Add(Key);
            }

            void OnPeerLeave(SignPublicKey Key)
            {
                lock (Peers)
                      Peers.Remove(Key);
            }

            lock (Messanger)
            {
                Messanger.OnPeerEntered += OnPeerEnter;
                Messanger.OnPeerLeaved += OnPeerLeave;
            }

            try
            {
                var TimeVal = TimeSpan.FromSeconds(Expiration);
                while (!Token.IsCancellationRequested)
                {
                    Messanger.InvitePeers(Expiration);
                    try { await Task.Delay(TimeVal, Token); }
                    catch
                    {
                        break;
                    }
                }
            }

            finally
            {
                lock (Messanger)
                {
                    Messanger.OnPeerEntered -= OnPeerEnter;
                    Messanger.OnPeerLeaved -= OnPeerLeave;
                }
            }

            return Peers.ToArray();
        }

        /// <summary>
        /// Invites other peers on the P2P network and wait their connection.
        /// </summary>
        /// <param name="Messanger"></param>
        /// <param name="Expiration"></param>
        /// <returns></returns>
        public static async Task<bool> InviteAndWaitAsync(this IMessanger Messanger, SignPublicKey Receiver, long Expiration = 5, CancellationToken Token = default)
        {
            var Tcs = new TaskCompletionSource();

            if (Messanger.IsConnectedDirectly(Receiver))
                return true;

            void OnPeerEnter(SignPublicKey Key)
            {
                if (Key == Receiver)
                {
                    lock (Messanger)
                        Tcs?.TrySetResult();
                }
            }

            void OnPeerLeave(SignPublicKey Key)
            {
                if (Key == Receiver && Tcs.Task.IsCompleted)
                {
                    lock (Messanger)
                        Tcs = new TaskCompletionSource();
                }
            }

            lock (Messanger)
            {
                Messanger.OnPeerEntered += OnPeerEnter;
                Messanger.OnPeerLeaved += OnPeerLeave;
            }

            try
            {
                var TimeVal = TimeSpan.FromSeconds(Expiration);
                while (!Token.IsCancellationRequested)
                {
                    if (Messanger.IsConnectedDirectly(Receiver))
                        return true;

                    Messanger.InvitePeer(Receiver, Expiration);

                    var Subtcs = new TaskCompletionSource();
                    using var Timeout = new CancellationTokenSource(TimeVal);
                    using var _1 = Token.Register(Timeout.Cancel);
                    using(Timeout.Token.Register(Subtcs.SetResult))
                    {
                        Task TaskTemp;

                        lock (Messanger)
                            TaskTemp = Tcs.Task;

                        await Task.WhenAny(Subtcs.Task, TaskTemp);
                    }
                }
            }

            finally
            {
                lock (Messanger)
                {
                    Messanger.OnPeerEntered -= OnPeerEnter;
                    Messanger.OnPeerLeaved -= OnPeerLeave;
                }
            }

            return Messanger.IsConnectedDirectly(Receiver);
        }
    }
}
