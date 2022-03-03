using Backrole.Crypto;
using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common.Modules;
using Glazer.P2P.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes
{
    internal class Application
    {
        /// <summary>
        /// Entry point of the Node App.
        /// </summary>
        /// <param name="Args"></param>
        /// <returns></returns>
        public static async Task Main(string[] Args)
        {
            var Host = new HostBuilder()
                .ConfigureWebHostDefaults(Http =>
                {
                    if (Debugger.IsAttached)
                    {
                        Args = Args.Concat(new string[] {
                            "--config", "node.json",
                            "--genesis", "genesis.json"
                        }).ToArray();
                    }

                    var Options = NodeOptions.Parse(Args,
                        Preview =>
                        {
                            Preview
                                .ModuleAssemblies
                                .Add(typeof(PreInit).Assembly);

                            Preview
                                .ModuleAssemblies
                                .Add(typeof(Genesis).Assembly);

                            Preview
                                .ModuleAssemblies
                                .Add(typeof(Multi).Assembly);
                        });

                    foreach (var Each in Options.ModuleInstances)
                        Each.ConfigureWebHostBuilder(Http, Options);

                    Http
                        .ConfigureLogging(Loggings =>
                        {
                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureLogging(Loggings, Options);
                        })
                        .ConfigureServices(Services =>
                        {
                            var Mvc = Services.AddControllers();

                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureServices(Services, Options);

                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureMvcBuilder(Mvc, Options);
                        })
                        .Configure((Web, App) =>
                        {
                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureApplicationBuilder(App, Options);

                            var Services = App.ApplicationServices;
                            var P2P = Services.GetRequiredService<IMessanger>();

                            lock (P2P)
                            {
                                foreach (var Each in Options.ModuleInstances)
                                    Each.ConfigureP2PMessanger(Services, P2P, Options);
                            }
                        });
                    
                })
                .Build();

            await Host.RunAsync();
        }

    }
}
