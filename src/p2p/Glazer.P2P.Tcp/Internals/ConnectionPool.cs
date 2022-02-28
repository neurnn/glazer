using Backrole.Crypto;
using Glazer.Common;
using Glazer.P2P.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Glazer.P2P.Tcp.Internals
{
    internal class ConnectionPool : HashSet<IConnection>
    {
        /// <summary>
        /// Check the connection is duplicated or not.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public bool Check(IConnection Connection)
        {
            lock (this)
            {
                if (Contains(Connection))
                    return true;

                return this.Count(X => X.PublicKey == Connection.PublicKey) > 0;
            }
        }

        /// <summary>
        /// Add the connection to the connection pool.
        /// </summary>
        /// <param name="Connection"></param>
        public new bool Add(IConnection Connection)
        {
            lock(this)
            {
                return base.Add(Connection);
            }
        }

        /// <summary>
        /// Remove the connection from the connection pool.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public new bool Remove(IConnection Connection)
        {
            lock(this)
            {
                return base.Remove(Connection);
            }
        }

        /// <summary>
        /// Find a connection by its public key.
        /// </summary>
        /// <param name="PublicKey"></param>
        /// <returns></returns>
        public IConnection Find(SignPublicKey PublicKey)
        {
            lock(this)
            {
                return this.FirstOrDefault(X => X.PublicKey == PublicKey);
            }
        }

        /// <summary>
        /// Find all connection using the predicate.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public IConnection[] FindAll(Predicate<IConnection> Connection)
        {
            lock(this)
            {
                return this.Where(X => Connection(X)).ToArray();
            }
        }

        /// <summary>
        /// Get all peer keys.
        /// </summary>
        /// <returns></returns>
        public SignPublicKey[] GetPeers()
        {
            lock (this)
            {
                return this.Select(X => X.PublicKey).ToArray();
            }
        }

        /// <summary>
        /// Send a message to connections.
        /// </summary>
        /// <param name="Message"></param>
        public void Send(Message Message)
        {
            if (Message.Sender.PublicKey == Message.Receiver)
                return;

            using var Writer = new PacketWriter();
            Message.Encode(Writer);

            if (Message.Receiver.IsValid)
            {
                var Dest = Find(Message.Receiver);
                if (Dest is not null)
                {
                    Dest.Emit(Writer);
                    return;
                }
            }

            foreach (var Each in FindAll(X => X.PublicKey != Message.Sender.PublicKey))
            {
                if (Each.PublicKey == Message.Sender.PublicKey)
                    continue;

                Each.Emit(Writer);
            }
        }
    }
}
