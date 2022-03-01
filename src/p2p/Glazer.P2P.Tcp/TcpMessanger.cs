using Backrole.Crypto;
using Glazer.P2P.Abstractions;
using Glazer.P2P.Protocols;
using Glazer.P2P.Tcp.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.P2P.Tcp
{
    public class TcpMessanger : IMessanger
    {
        private static readonly object[] EMPTY_ARGS = new object[0];

        private Channel<Message> m_Channel;
        private TcpListener m_Listener;
        private Task m_Task;

        private MessageBox m_MessageBox = new MessageBox();
        private ConnectionPool m_Pool = new();

        private ContactBook m_Contacts = new();
        private List<IMessangerProtocol> m_Protocols = new();

        /// <summary>
        /// Initialize a new <see cref="TcpMessanger"/> instance,
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <param name="KeyPair"></param>
        public TcpMessanger(IPEndPoint Endpoint, SignKeyPair KeyPair = default)
        {
            (m_Listener = new TcpListener(Endpoint)).Start();

            if (!KeyPair.IsValid)
                KeyPair = Signs.Default.Get("secp256k1").MakeKeyPair();

            this.Endpoint = Endpoint;
            this.KeyPair = KeyPair;

            m_Channel = Channel.CreateUnbounded<Message>();
            m_Task = Task.Factory.StartNew(RunLoop, TaskCreationOptions.LongRunning);
            SetProtocolDefaults();
        }

        /// <summary>
        /// Create a new <see cref="TcpMessanger"/> that uses random port.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="KeyPair"></param>
        /// <returns></returns>
        public static TcpMessanger RandomPort(IPAddress Address, SignKeyPair KeyPair = default, ushort InitialPort = 7000)
        {
            while(true)
            {
                try { return new TcpMessanger(new IPEndPoint(Address, InitialPort), KeyPair); }
                catch
                {
                }

                InitialPort = BitConverter.ToUInt16(Rng.Make(2, true));
            }
        }

        /// <summary>
        /// Set the protocol default implementations.
        /// </summary>
        private void SetProtocolDefaults()
        {
            var Protocols = typeof(InvitePeers).Assembly.GetTypes()
                .Where(X => !X.IsAbstract && X.IsAssignableTo(typeof(IMessangerProtocol)));

            foreach (var Each in Protocols)
            {
                var Ctor = Each.GetConstructor(Type.EmptyTypes);
                if (Ctor is null) continue;

                m_Protocols.Add(Ctor.Invoke(EMPTY_ARGS) as IMessangerProtocol);
            }
        }

        /// <inheritdoc/>
        public IMessanger Use<TProtocol>() where TProtocol : IMessangerProtocol, new()
        {
            lock (m_Protocols)
            {
                if (m_Protocols.FindIndex(X => X is TProtocol) >= 0)
                    return this;

                m_Protocols.Add(new TProtocol());
                return this;
            }
        }

        /// <inheritdoc/>
        public SignKeyPair KeyPair { get; }

        /// <inheritdoc/>
        public IPEndPoint Endpoint { get; }

        /// <inheritdoc/>
        public event Action<SignPublicKey> OnPeerEntered;

        /// <inheritdoc/>
        public event Action<SignPublicKey> OnPeerLeaved;

        /// <inheritdoc/>
        public bool IsConnectedDirectly(SignPublicKey Target)
        {
            return m_Pool.Find(Target) != null;
        }

        /// <inheritdoc/>
        public SignPublicKey[] GetDirectPeers()
        {
            return m_Pool.GetPeers();
        }

        /// <summary>
        /// Get the connection pool.
        /// </summary>
        /// <returns></returns>
        internal ConnectionPool GetPool() => m_Pool;

        /// <summary>
        /// Get the contacts.
        /// </summary>
        /// <returns></returns>
        internal ContactBook GetContacts() => m_Contacts;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PubKey"></param>
        internal void NotifyPeerEvent(SignPublicKey PubKey, bool State)
        {
            lock(this)
            {
                if (State)
                    OnPeerEntered?.Invoke(PubKey);

                else
                    OnPeerLeaved?.Invoke(PubKey);
            }
        }

        /// <inheritdoc/>
        public IMessanger Contact(IPEndPoint Endpoint)
        {
            if (m_Contacts.Set(Endpoint))
            {
                _ = new ContactConnection(this, Endpoint).RunAsync(m_Channel.Writer);
            }

            return this;
        }

        /// <inheritdoc/>
        public IMessanger Emit(Message Message)
        {
            if (!Message.Sender.IsValid)
                 Message.Sign(KeyPair);

            if (m_MessageBox.Check(Message))
                return this;

            m_MessageBox.Push(Message);
            m_Pool.Send(Message);
            return this;
        }

        /// <inheritdoc/>
        public async Task<Message> WaitAsync(CancellationToken Token = default)
        {
            while (true)
            {
                var Message = await m_Channel.Reader.ReadAsync(Token);
                if (Message is not null)
                {
                    if (m_MessageBox.Check(Message) || Handle(Message))
                        continue;

                    if (Message.Sender.PublicKey != KeyPair.PublicKey)
                        Emit(Message);

                    else
                    {
                        m_MessageBox.Push(Message);
                        continue;
                    }

                    return Message;
                }

                Token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Handle the system messages.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        private bool Handle(Message Message)
        {
            lock (m_Protocols)
            {
                foreach (var Each in m_Protocols)
                {
                    if (Each.Handle(this, Message))
                        return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try { m_Listener.Stop(); } catch { }

            await m_Task.ConfigureAwait(false);
            await DisposeProtocols().ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose the protocol extensions.
        /// </summary>
        /// <returns></returns>
        private async Task DisposeProtocols()
        {
            while (true)
            {
                var Queue = new Queue<object>();
                lock (m_Protocols)
                {
                    if (m_Protocols.Count <= 0)
                        break;

                    foreach (var Each in m_Protocols)
                        Queue.Enqueue(Each);

                    m_Protocols.Clear();
                }

                while (Queue.TryDequeue(out var Each))
                {
                    if (Each is IAsyncDisposable Async)
                        await Async.DisposeAsync().ConfigureAwait(false);

                    else if (Each is IDisposable Sync)
                        Sync.Dispose();
                }
            }
        }

        /// <summary>
        /// Run the messanger loop.
        /// </summary>
        private void RunLoop()
        {
            using var Cts = new CancellationTokenSource();
            while (true)
            {
                Socket Socket;

                try
                {
                    if ((Socket = m_Listener.AcceptSocket()) is null)
                        continue;
                }
                catch
                {
                    Cts.Cancel();
                    break;
                }

                _ = new Connection(this, Socket).RunAsync(m_Channel.Writer, Cts.Token);
            }

            m_Channel.Writer.TryComplete();
            m_MessageBox.Dispose();
        }
    }
}
