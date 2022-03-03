using Backrole.Crypto;
using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common.Internals;
using Glazer.Nodes.Common.Internals.Engine;
using Glazer.P2P.Abstractions;
using Glazer.P2P.Tcp;
using Glazer.Storage.Abstraction;
using Glazer.Storage.Integration.AspNetCore;
using Glazer.Storage.Sqlite;
using Glazer.Transactions.Integration.AspNetCore;
using Glazer.Transactions.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Glazer.Nodes.Common.Modules
{
    public sealed class PreInit : NodeModule<PreInit>
    {
        /// <inheritdoc/>
        public override int Priority => int.MinValue;

        /// <inheritdoc/>
        public override void ConfigureWebHostBuilder(IWebHostBuilder Host, NodeOptions Options)
        {
            var Endpoint = MakeHttpEndpoint(Options.HttpEndpoint);
            Host
                .UseKestrel()
                .UseUrls($"http://{Endpoint}/");
        }

        /// <summary>
        /// Make <see cref="IPEndPoint"/> from the endpoint string.
        /// </summary>
        /// <param name="EndpointStr"></param>
        /// <returns></returns>
        private IPEndPoint MakeHttpEndpoint(string EndpointStr)
        {
            var Endpoint = IPEndPoint.Parse(EndpointStr);
            if (Debugger.IsAttached)
            {
                while (true)
                {
                    TcpListener Tcp = null;
                    try
                    {
                        (Tcp = new TcpListener(Endpoint)).Start();
                        break;
                    }
                    catch
                    {
                        var NewPort = BitConverter.ToUInt16(Rng.Make(2, true));
                        Endpoint = new IPEndPoint(Endpoint.Address, NewPort);
                    }

                    finally
                    {
                        try
                        {
                            if (Tcp is not null)
                                Tcp.Stop();
                        }
                        catch { }
                    }
                }
            }

            return Endpoint;
        }

        /// <inheritdoc/>
        public override void ConfigureLogging(ILoggingBuilder Logging, NodeOptions Options)
        {
            Logging
                .AddConsole();
        }

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection Services, NodeOptions Options)
        {
            if (!Directory.Exists(Options.BlockDir))
                Directory.CreateDirectory(Options.BlockDir);

            if (!Directory.Exists(Options.StateDir))
                Directory.CreateDirectory(Options.StateDir);

            Services
                .AddSingleton(Options)
                .AddSingleton<INodeLifetime, NodeLifetime>()
                .AddSingleton<INodeEngineWorker, NodeEngineWorker>()
                .AddHostedService<NodeEntryPoint>()
                .AddHostedService<NodeEngineWorker.Service>();

            Services /* Block Storage & Transaction Sets. */
                .SetBlockStorage(App => new SqliteStorage(Options.BlockDir))
                .SetTransactionSets(App =>
                {
                    var Blocks = App.GetRequiredService<IStorage>();
                    return new SqliteTransactionSets(Blocks, Options.StateDir);
                });

            Services
                .AddSingleton<INodeEngineManager, NodeEngineManager>()
                .GetNodeEngineFactory();

            ConfigureP2PMessanger(Services, Options);
        }

        /// <summary>
        /// Configure the <see cref="IMessanger"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Options"></param>
        private void ConfigureP2PMessanger(IServiceCollection Services, NodeOptions Options)
        {
            var Endpoint = IPEndPoint.Parse(Options.P2PEndpoint);
            var Seeds = Options.P2PSeeds.Select(X => IPEndPoint.Parse(X)).ToArray();

            Services
                .AddSingleton<IMessanger>(Services =>
                {
                    if (Debugger.IsAttached)
                    {
                        var Port = (ushort)Math.Min(Endpoint.Port, ushort.MaxValue - 1);
                        var Instance = TcpMessanger.RandomPort(Services, Endpoint.Address, default, Port);

                        if (Endpoint.Port != Port)
                            Instance.Contact(new IPEndPoint(IPAddress.Loopback, Endpoint.Port));

                        foreach (var Each in Seeds)
                            Instance.Contact(Each);

                        return Instance;
                    }

                    return new TcpMessanger(Services, Endpoint);
                });
        }

        /// <inheritdoc/>
        public override void ConfigureP2PMessanger(IServiceProvider App, IMessanger P2P, NodeOptions Options)
        {
            var Logger = App.GetService<ILogger<IMessanger>>();
            if (Logger != null)
            {
                P2P.OnPeerEntered += Pub => Logger.LogInformation($"Node attached: {Pub.Value.ToBase58()}");
                P2P.OnPeerEntered += Pub => Logger.LogInformation($"Node detached: {Pub.Value.ToBase58()}");

                Logger.LogInformation($"P2P messanger is running on: {P2P.Endpoint}.");
            }
        }

        /// <inheritdoc/>
        public override void ConfigureMvcBuilder(IMvcBuilder Mvc, NodeOptions Options)
        {
            Mvc
                .AddBlockStorageApiControllers()
                .AddTransactionSetApiController();
        }

        /// <inheritdoc/>
        public override void ConfigureApplicationBuilder(IApplicationBuilder Http, NodeOptions Options)
        {
            if (Debugger.IsAttached)
                Http.UseDeveloperExceptionPage();

            Http
                .UseRouting()
                .UseAuthorization();
        }
    }
}
