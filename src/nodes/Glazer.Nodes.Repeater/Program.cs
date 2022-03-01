using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Glazer.P2P.Hosting;
using Glazer.P2P.Integration.AspNetCore;
using System;
using System.Net;
using System.Diagnostics;
using Glazer.P2P.Tcp;
using System.Threading.Tasks;
using System.Threading;
using Glazer.P2P.Abstractions;
using Glazer.P2P.Protocols;
using System.Linq;
using System.Text;
using Backrole.Crypto;

namespace Glazer.Nodes.Repeater
{
    class Program : BackgroundService
    {
        private ILogger m_AppLogger;
        private ILogger m_MsgLogger;
        private IMessanger m_Messanger;

        public class Accessor
        {
            public TaskCompletionSource TCS = new TaskCompletionSource();
            public ILogger MsgLogger;
        }

        /// <summary>
        /// Run the P2P repeater application.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var Host = new HostBuilder()
                .ConfigureLogging(X => X.AddConsole())
                .ConfigureServices(App =>
                {
                    var Accessor = new Accessor();

                    App.AddSingleton(Accessor);
                    App
                        .AddP2PHostService()
                        .SetEndpoint(new IPEndPoint(IPAddress.Any, 7000))
                        .SetFactory((Endpoint, KeyPair) =>
                        {
                            var Port = (ushort)Math.Min(Endpoint.Port, ushort.MaxValue - 1);
                            var Instance = TcpMessanger.RandomPort(Endpoint.Address, KeyPair, Port);

                            if (Instance.Endpoint.Port != Port)
                                Instance.Contact(new IPEndPoint(IPAddress.Loopback, Endpoint.Port));

                            return Instance;
                        })
                        .Use(async (_, Message, Next) =>
                        {
                            await Accessor.TCS.Task;
                            LogMessage(Accessor.MsgLogger, Message);
                            await Next();
                        });

                    App
                        .AddHostedService<Program>();
                })
                .Build();

            await Host.RunAsync();
        }

        /// <summary>
        /// Initialize a new <see cref="Program"/> instance.
        /// </summary>
        /// <param name="Logger"></param>
        public Program(ILoggerFactory LoggerFactory, IMessangerHost Host, Accessor Accessor)
        {
            m_AppLogger = LoggerFactory.CreateLogger("P2P Message");
            m_MsgLogger = LoggerFactory.CreateLogger("P2P Repeater");
            m_Messanger = Host.Messanger;

            AttachLoggerToMessanger(Accessor);
        }

        /// <summary>
        /// Attach a logger to messanger instance.
        /// </summary>
        private void AttachLoggerToMessanger(Accessor Accessor)
        {
            lock (m_Messanger)
            {
                m_Messanger.OnPeerEntered
                    += Who => m_AppLogger.LogInformation($"Node attached: `{Who}`.");

                m_Messanger.OnPeerLeaved
                    += Who => m_AppLogger.LogInformation($"Node detached: `{Who}`.");

                foreach(var Who in m_Messanger.GetDirectPeers())
                {
                    m_AppLogger.LogInformation($"Node attached: `{Who}`.");
                }

                Accessor.MsgLogger = m_MsgLogger;
                Accessor.TCS.TrySetResult();
            }
        }

        /// <summary>
        /// Execute the <see cref="Program"/>.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken Token)
        {
            m_AppLogger.LogInformation($"Glazer Node Repeater is started for {m_Messanger.Endpoint}.");

            return Task.Delay(Timeout.Infinite, Token);
        }

        private static void LogMessage(ILogger MessageLogger, Message Message)
        {
            var Log = new StringBuilder();

            Log.AppendLine($"MESSAGE {Message.Type}");
            Log.AppendLine($"EXPIRATION {Message.Expiration.ToDateTime().ToString("r")}");
            Log.AppendLine($"FROM {Message.Sender.PublicKey.Value.ToBase58()} ({Message.Sender.Signature.Value.ToBase58()})");

            if (Message.Receiver.IsValid)
                Log.AppendLine($"TO {Message.Receiver.Value.ToBase58()}");

            foreach (var Header in Message.Headers.Where(X => !X.Key.Equals("Type")))
                Log.AppendLine($"> {Header.Key}: {Header.Value}.");

            if (Message.Data != null && Message.Data.Length > 0)
            {
                Log.AppendLine($"PAYLOAD ({Message.Data.Length} bytes)");
                Log.AppendLine(string.Join('\n', BitConverter
                    .ToString(Message.Data).Split('-')
                    .Select((X, i) => (Index: i, Hex: X)).GroupBy(X => X.Index / 32)
                    .Select(X => $"{(X.Key * 32).ToString("x8")} {string.Join(' ', X.Select(Y => Y.Hex))}")));
            }

            MessageLogger.LogInformation("{0}", Log.ToString().Trim());
        }
    }
}
