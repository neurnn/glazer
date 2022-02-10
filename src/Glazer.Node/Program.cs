using Backrole.Core;
using Backrole.Core.Abstractions;
using Backrole.Core.Abstractions.Defaults;
using Backrole.Core.Builders;
using Backrole.Core.Configurations.Json;
using Glazer.Core;
using Glazer.Core.Exceptions;
using Glazer.Core.Nodes;
using Glazer.Core.Models;
using Glazer.Core.Models.Settings;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Node
{
    class Program
    {
        /// <summary>
        /// Entry point of the app.
        /// </summary>
        /// <param name="Args"></param>
        /// <returns></returns>
        static async Task<int> Main(string[] Args)
        {
            var Host = new HostBuilder()
                .Configure<NodeArguments>((_, Options) =>
                {
                    Options.AddRange(Args);
                    Options.Add("--genesis");
                })

                .ConfigureConfigurations(Configs =>
                {
                    var Name = "glnode.json";
                    var Index = Array.IndexOf(Args, "--settings");
                    if (Index >= 0)
                    {
                        if (Args.Length <= Index + 1)
                            throw new IncompletedException("`--settings` requires file name. (e.g. --settings glnode.json)");

                        Name = Args[Index + 1];
                    }

                    Configs
                        .AddEnvironmentVariables()
                        .AddJsonFile(Name, Options => Options.AsLowerCase = true);
                })

                .Configure<HttpSettings>((Configs, Http) => Http.From(Configs))
                .Configure<LocalNodeSettings>((Configs, LocalNode) => LocalNode.From(Configs))
                .Configure<DiscoverySettings>((Configs, Discovery) => Discovery.From(Configs))
                .Configure<PeerNetworkSettings>((Configs, PeerNetwork) => PeerNetwork.From(Configs))

                .ConfigureLoggings(Loggings =>
                {
                    Loggings
                        .AddConsole(Options =>
                        {
                            Options.PrettyPrint = Array.IndexOf(Args, "--no-fancy") < 0;
                            Options.DebugLogsOnlyWithDebugger = Array.IndexOf(Args, "--debug-logs") < 0;
                        });
                })
                .ConfigureServices(Services =>
                {
                    LocalNode.SetServices(Services);

                    Services
                        .AddSingleton<MessageMapper>(X =>
                        {
                            var Mapper = new MessageMapper()
                                .Map(typeof(Program).Assembly);

                            return Mapper;
                        });
                });

            await Host.Build().RunAsync();
            return 0;
        }

    }
}
