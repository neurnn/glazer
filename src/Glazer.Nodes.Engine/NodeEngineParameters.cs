using Backrole.Core;
using Backrole.Core.Abstractions;
using Backrole.Core.Abstractions.Defaults;
using Backrole.Core.Builders;
using Backrole.Crypto;
using Glazer.Nodes.Contracts.Chains;
using Glazer.Nodes.Contracts.Discovery;
using Glazer.Nodes.Contracts.Storages;
using Glazer.Nodes.Contracts.Storages.Implementations;
using Glazer.Nodes.Contracts.Trackers;
using Glazer.Nodes.Exceptions;
using Glazer.Nodes.Models;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using static Glazer.Nodes.Helpers.ModelHelpers;

namespace Glazer.Nodes.Engine
{
    public class NodeEngineParameters
    {
        private IServiceCollection m_Services;
        private IConfigurationBuilder m_Configurations;
        private ILoggerFactoryBuilder m_LoggerFactoryBuilder;

        private static string InstalledPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// Run the node as genesis mode or not.
        /// Note that this option forcibly disables some incompatible features.
        /// </summary>
        public bool RunAsGenesis { get; set; }

        /// <summary>
        /// Capacity of the request queue.
        /// </summary>
        public int RequestQueueCapacity { get; set; } = 1024;

        /// <summary>
        /// Accounts to be recorded for all transactions and 
        /// blocks processed by this node, signature information, etc.
        /// </summary>
        public Account Account { get; set; }

        /// <summary>
        /// Private key for the <see cref="Account"/>.
        /// </summary>
        public SignPrivateKey PrivateKey { get; set; }

        /// <summary>
        /// Service Collection that is to extends the node engine.
        /// </summary>
        public IServiceCollection Services
        {
            get => OnDemand(ref m_Services, () => new ServiceCollection());
            set => Assigns(ref m_Services, value, _ => m_LoggerFactoryBuilder = null);
        }

        /// <summary>
        /// Logging factory builder.
        /// </summary>
        public ILoggerFactoryBuilder Loggings 
            => OnDemand(ref m_LoggerFactoryBuilder, () => new LoggerFactoryBuilder(Services).AddConsole());

        /// <summary>
        /// Configuration Builder.
        /// </summary>
        public IConfigurationBuilder Configurations
        {
            get => OnDemand(ref m_Configurations, () => new ConfigurationBuilder());
            set => m_Configurations = value;
        }

        /// <summary>
        /// Local P2P Endpoint.
        /// To disable P2P listener, set null.
        /// </summary>
        public IPEndPoint LocalP2PEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 7000);

        /// <summary>
        /// Clone the <see cref="Services"/>.
        /// </summary>
        /// <returns></returns>
        private IServiceCollection CloneServices()
        {
            var Services = new ServiceCollection();
            if (m_Services != null)
            {
                foreach (var Each in m_Services)
                    Services.Add(Each);
            }
            return Services;
        }

        /// <summary>
        /// Clone the <see cref="Configurations"/>.
        /// </summary>
        /// <returns></returns>
        private IConfigurationBuilder CloneConfigurations()
        {
            var Configurations = new ConfigurationBuilder();
            var Original = m_Configurations.Build();

            foreach(var Key in Original.Keys)
                Configurations.Set(Key, Original[Key]);

            return Configurations;
        }

        /// <summary>
        /// Clone the <see cref="NodeEngineParameters"/>.
        /// </summary>
        /// <returns></returns>
        public NodeEngineParameters Clone() => new NodeEngineParameters
        {
            Account = Account,
            PrivateKey = PrivateKey,
            RequestQueueCapacity = RequestQueueCapacity,
            Services = CloneServices(),
            Configurations = CloneConfigurations(),
            LocalP2PEndPoint = LocalP2PEndPoint
        };

        /// <summary>
        /// Check the account and its private key is valid or not.
        /// </summary>
        internal void CheckParameters()
        {
            if (!Account.IsValid)
                throw new PreconditionFailedException("the node engine requires the account.");

            if (!PrivateKey.IsValid)
                throw new PreconditionFailedException("the node engine requires the private key of the account.");

            var Data = Rng.Make(32);
            var Sign = Signs.Default.Sign(PrivateKey, Data);

            if (!Signs.Default.Verify(Account.PublicKey, Sign, Data))
                throw new InvalidOperationException("the account and its private key is not a valid key pair.");
        }
    }

}
