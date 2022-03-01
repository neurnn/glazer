using Backrole.Crypto;
using Glazer.Kvdb.Integration.AspNetCore;
using Glazer.Nodes.Abstractions;
using Glazer.P2P.Hosting;
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

namespace Glazer.Nodes.Modules
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
                .AddSingleton(Options);

            Services /* Block Storage & Transaction Sets. */
                .SetBlockStorage(App => new SqliteStorage(Options.BlockDir))
                .SetTransactionSets(App =>
                {
                    var Blocks = App.GetRequiredService<IStorage>();
                    return new SqliteTransactionSets(Blocks, Options.StateDir);
                });
        }

        /// <inheritdoc/>
        public override void ConfigureMvcBuilder(IMvcBuilder Mvc, NodeOptions Options)
        {
            Mvc
                .AddBlockStorageApiControllers()
                .AddTransactionSetApiController();
        }

        /// <inheritdoc/>
        public override void ConfigureP2PHostService(IMessangerHostBuilder P2P, NodeOptions Options)
        {
            P2P
                .With(Options.P2PSeeds.Select(X => IPEndPoint.Parse(X)).ToArray())
                .SetEndpoint(IPEndPoint.Parse(Options.P2PEndpoint)).Set(default)
                .SetFactory((Endpoint, KeyPair) =>
                {
                    if (Debugger.IsAttached)
                    {
                        var Port = (ushort)Math.Min(Endpoint.Port, ushort.MaxValue - 1);
                        var Instance = TcpMessanger.RandomPort(Endpoint.Address, KeyPair, Port);

                        P2P.With(new IPEndPoint(IPAddress.Loopback, Endpoint.Port)); // --> Adds the endpoint as initial peer.
                        return Instance;
                    }

                    return new TcpMessanger(Endpoint, KeyPair);
                })
                .WhenEnter(Who =>
                {
                    Console.WriteLine($"Enter: {Who}");
                })
                .WhenLeave(Who =>
                {
                    Console.WriteLine($"Leave: {Who}");
                });
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
