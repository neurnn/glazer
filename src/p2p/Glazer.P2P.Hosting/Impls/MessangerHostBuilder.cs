using Backrole.Crypto;
using Glazer.P2P.Abstractions;
using Glazer.P2P.Tcp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Glazer.P2P.Hosting.Impls
{
    public class MessangerHostBuilder : IMessangerHostBuilder
    {
        private Queue<Action<IMessanger>> m_Configures = new();
        private Queue<Action<IMessanger>> m_PostConfigures = new();
        private Func<IPEndPoint, SignKeyPair, IMessanger> m_Factory;
        private Func<IMessanger, Message, Func<Task>, Task> m_Handler;
        private SignKeyPair m_KeyPair = default;
        private IPEndPoint m_Endpoint;

        /// <inheritdoc/>
        public IMessangerHostBuilder Use<TProtocol>() where TProtocol : IMessangerProtocol, new()
        {
            m_Configures.Enqueue(X => X.Use<TProtocol>());
            return this;
        }

        /// <inheritdoc/>
        public IMessangerHostBuilder WhenEnter(Action<SignPublicKey> OnEntered)
        {
            m_Configures.Enqueue(X => X.OnPeerEntered += OnEntered);
            return this;
        }

        /// <inheritdoc/>
        public IMessangerHostBuilder WhenLeave(Action<SignPublicKey> OnLeaved)
        {
            m_Configures.Enqueue(X => X.OnPeerLeaved += OnLeaved);
            return this;
        }

        /// <inheritdoc/>
        public IMessangerHostBuilder Use(Func<IMessanger, Message, Func<Task>, Task> Handler)
        {
            if (m_Handler is null)
            {
                m_Handler = Handler;
                return this;
            }

            var Previous = m_Handler;
            m_Handler = (Messanger, Msg, Next) =>
            {
                return Previous(Messanger, Msg, () => Handler(Messanger, Msg, Next));
            };

            return this;
        }

        /// <inheritdoc/>
        public IMessangerHostBuilder With(params IPEndPoint[] InitialContacts)
        {
            m_PostConfigures.Enqueue(X =>
            {
                foreach (var Each in InitialContacts)
                    X.Contact(Each);
            });

            return this;
        }

        /// <inheritdoc/>
        public IMessangerHostBuilder Set(SignKeyPair KeyPair)
        {
            m_KeyPair = KeyPair;
            return this;
        }

        /// <inheritdoc/>
        public IMessangerHostBuilder SetFactory(Func<IPEndPoint, SignKeyPair, IMessanger> Factory)
        {
            m_Factory = Factory;
            return this;
        }

        /// <inheritdoc/>
        public IMessangerHostBuilder SetEndpoint(IPEndPoint Endpoint)
        {
            m_Endpoint = Endpoint;
            return this;
        }

        /// <inheritdoc/>
        public IMessangerHost Build()
        {
            var Factory = m_Factory ?? DEFAULT_FACTORY;
            var Messanger = Factory(m_Endpoint, m_KeyPair);

            while (m_Configures.TryDequeue(out var Each))
                Each?.Invoke(Messanger);

            while (m_PostConfigures.TryDequeue(out var Each))
                Each?.Invoke(Messanger);

            return new MessangerHost(Messanger, m_Handler);
        }

        /// <summary>
        /// Default factory.
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <param name="KeyPair"></param>
        /// <returns></returns>
        private static IMessanger DEFAULT_FACTORY(IPEndPoint Endpoint, SignKeyPair KeyPair)
        {
            if (Endpoint is null)
                return TcpMessanger.RandomPort(IPAddress.Any, KeyPair);

            return new TcpMessanger(Endpoint, KeyPair);
        }
    }
}
