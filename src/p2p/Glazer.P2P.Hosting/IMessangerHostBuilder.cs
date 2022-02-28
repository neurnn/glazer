using Backrole.Crypto;
using Glazer.P2P.Abstractions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Glazer.P2P.Hosting
{
    public interface IMessangerHostBuilder
    {
        /// <summary>
        /// Adds the protocol extension to the host.
        /// </summary>
        /// <typeparam name="TProtocol"></typeparam>
        /// <returns></returns>
        IMessangerHostBuilder Use<TProtocol>() where TProtocol : IMessangerProtocol, new();

        /// <summary>
        /// Adds a delegate that invoked when the other peer entered to the host.
        /// </summary>
        /// <param name="OnEntered"></param>
        /// <returns></returns>
        IMessangerHostBuilder WhenEnter(Action<SignPublicKey> OnEntered);

        /// <summary>
        /// Adds a delegate that invoked when the other peer leaved from the host.
        /// </summary>
        /// <param name="OnLeaved"></param>
        /// <returns></returns>
        IMessangerHostBuilder WhenLeave(Action<SignPublicKey> OnLeaved);

        /// <summary>
        /// Adds a delegate that handles <see cref="Message"/> instance.
        /// </summary>
        /// <param name="Handler"></param>
        /// <returns></returns>
        IMessangerHostBuilder Use(Func<IMessanger, Message, Func<Task>, Task> Handler);

        /// <summary>
        /// Adds initial contacts to connect.
        /// </summary>
        /// <param name="InitialContacts"></param>
        /// <returns></returns>
        IMessangerHostBuilder With(params IPEndPoint[] InitialContacts);

        /// <summary>
        /// Sets the <see cref="SignKeyPair"/> that is used to identify each others.
        /// </summary>
        /// <param name="KeyPair"></param>
        /// <returns></returns>
        IMessangerHostBuilder Set(SignKeyPair KeyPair);

        /// <summary>
        /// Sets the <see cref="IMessanger"/> factory delegate.
        /// </summary>
        /// <param name="Factory"></param>
        /// <returns></returns>
        IMessangerHostBuilder SetFactory(Func<IPEndPoint, SignKeyPair, IMessanger> Factory);

        /// <summary>
        /// Sets the <see cref="IPEndPoint"/> to listen other peers.
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <returns></returns>
        IMessangerHostBuilder SetEndpoint(IPEndPoint Endpoint);

        /// <summary>
        /// Build the <see cref="IMessangerHost"/> instance.
        /// </summary>
        /// <returns></returns>
        IMessangerHost Build();
    }
}
