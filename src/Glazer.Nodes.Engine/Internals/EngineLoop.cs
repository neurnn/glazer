using Backrole.Core;
using Backrole.Core.Abstractions;
using Glazer.Nodes.Contracts;
using Glazer.Nodes.Contracts.Chains;
using Glazer.Nodes.Contracts.Discovery;
using Glazer.Nodes.Contracts.Storages;
using Glazer.Nodes.Contracts.Storages.Implementations;
using Glazer.Nodes.Contracts.Trackers;
using Glazer.Nodes.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Glazer.Nodes.Engine.Internals
{
    internal class EngineLoop : IServiceProvider
    {
        private ServiceProvider m_Services;
        private NodeEngineLifetime m_Lifetime;
        private Channel<NodeRequest> m_Channel;
        private Task m_Loop;
        private ILogger m_Logger;

        /// <summary>
        /// Initialize a new <see cref="EngineLoop"/>.
        /// </summary>
        /// <param name="Parameters"></param>
        public EngineLoop(NodeEngine Engine, NodeEngineParameters Parameters)
        {
            EnableServiceProvider(Engine, this.Parameters = Parameters);

            m_Logger = m_Services.GetRequiredService<ILogger<NodeEngine>>();
            m_Lifetime = m_Services.GetRequiredService<NodeEngineLifetime>();
            m_Loop = RunAsync();
        }

        /// <summary>
        /// Enable the <see cref="ServiceProvider"/>.
        /// </summary>
        /// <param name="Engine"></param>
        /// <param name="Parameters"></param>
        private void EnableServiceProvider(NodeEngine Engine, NodeEngineParameters Parameters)
        {
            Parameters.Services /* Set instances. */
                .AddSingleton(Parameters.Configurations.Build(), true)
                .AddSingleton(Parameters.Account, true)
                .AddSingleton(Parameters.Account.PublicKey, true)
                .AddSingleton(Parameters.PrivateKey, true)
                .AddSingleton(Engine, true)         // Engine instance.
                .AddSingleton(Engine.Node, true)    // Engine's Opaque Node instance.
                .AddSingleton(Parameters, true)     // Engine Parameters.
                .AddSingleton(this, true);          // Engine Loop instance (this).

            Parameters.Services /* Set factory delegates. */
                .AddSingleton<ILoggerFactory>(Parameters.Loggings.Build)
                .AddSingleton<NodeEngineLifetime>(X => new NodeEngineLifetime(Engine, m_Services));

            SetDefault<StorageFeature, MemoryStorageFeature>(Parameters);

            m_Channel = Channel.CreateBounded<NodeRequest>(Parameters.RequestQueueCapacity);
            m_Services = new ServiceProvider(Parameters.Services);
        }

        private static void SetDefault<NodeType, ImplType>(NodeEngineParameters Parameters)
        {
            if (Parameters.Services.Find<NodeType>() is null)
                Parameters.Services.AddSingleton<NodeType, ImplType>();
        }

        /// <summary>
        /// Engine Parameters.
        /// </summary>
        public NodeEngineParameters Parameters { get; }

        /// <summary>
        /// Wait for the engine loop to complete.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task WaitAsync(CancellationToken Token)
        {
            var Tcs = new TaskCompletionSource();

            using var _1 = Token.Register(() => Tcs.TrySetCanceled());
            using var _2 = m_Lifetime.Stopping.Register(() => Tcs.TrySetResult());

            await Tcs.Task;
        }

        /// <summary>
        /// Executes a request on the engine loop.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public async Task<NodeResponse> ExecuteAsync(NodeRequest Request)
        {
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Request.Aborted);
            var Tcs = new TaskCompletionSource<NodeResponse>();

            using (Cts.Token.Register(() => Tcs.TrySetCanceled()))
            {
                /* Reply redirector. */
                Task OnReply(NodeRequest R, NodeResponse Reply)
                {
                    Tcs.TrySetResult(Reply);
                    return R.ReplyAsync(Reply);
                }

                /* Copy properties to opaque. */
                var Opaque = new NodeRequest(OnReply, Cts.Token)
                {
                    Node = Request.Node,
                    Expiration = Request.Expiration,
                    Properties = Request.Properties,
                    Headers = Request.Headers,
                    Options = Request.Options,
                    Sender = Request.Sender,
                    SenderSign = Request.SenderSign,
                    Message = Request.Message
                };

                try { await m_Channel.Writer.WriteAsync(Opaque, Opaque.Aborted); }
                catch
                {
                    throw new InvalidOperationException("the node engine has been stopped.");
                }

                try { return await Tcs.Task; }
                catch(OperationCanceledException)
                {
                    var Reply = new NodeResponse();
                    await Request.ReplyAsync(Reply);
                    return Reply;
                }

                finally { Cts.Cancel(); }
            }
        }

        /// <summary>
        /// Terminates the engine loop asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task TerminateAsync()
        {
            m_Channel.Writer.TryComplete();
            return m_Loop;
        }

        /// <summary>
        /// Gets the service by its type.
        /// </summary>
        /// <param name="ServiceType"></param>
        /// <returns></returns>
        public object GetService(Type ServiceType) => m_Services.GetService(ServiceType);

        /// <summary>
        /// Run the engine loop asynchronously.
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync()
        {
            using var Cts = new CancellationTokenSource();
            var Background = m_Services.GetService<IHostedService>();
            var Request = null as NodeRequest;
            var LocalNodes = null as NodeFeature[];
            var Subscriptions = new Stack<IDisposable>();

            m_Logger.Info("Initializing the glazer node engine...");
            try
            {
                /* To ensure local node feature instances are initialized. */
                LocalNodes = new NodeFeature[]
                {
                    m_Services.GetService<ChainFeature>(),
                    m_Services.GetService<StorageFeature>(),
                    m_Services.GetService<TrackerFeature>(),
                    m_Services.GetService<DiscoveryFeature>(),
                    m_Services.GetService<EndpointFeature>(),
                    m_Services.GetService<RoutingFeature>()
                }
                .Where(X => X != null)
                .ToArray();

                m_Logger.Info("Starting the glazer node engine...");

                /* Starts the hosted services. */
                if (Background != null)
                    await Background.StartAsync();

                m_Lifetime.NotifyStarted();
                m_Logger.Info("Waiting the incoming requests...");

                foreach (var Each in LocalNodes)
                    Subscriptions.Push(Each.SubscribeRequests(ExecuteAsync));

                while (true)
                {
                    try { Request = await m_Channel.Reader.ReadAsync(); }
                    catch
                    {
                        Cts.Cancel();
                        break;
                    }

                    // TODO: Routing the requests to local node?
                }
            }

            finally
            {
                while (Subscriptions.TryPop(out var Each))
                    Each?.Dispose();

                m_Lifetime.Stop();
                m_Logger.Info("Stopping the node engine...");

                if (Background != null)
                    await Background.StopAsync();

                m_Lifetime.NotifyStopped();
                m_Logger.Info("Okay, the node engine stopped.");

            }

            m_Lifetime.Dispose();
            m_Services.Dispose();
            LocalNodes = null;
        }
    }
}
