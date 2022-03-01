using Glazer.Kvdb.Integration.AspNetCore;
using Glazer.Nodes.Abstractions;
using Glazer.P2P.Integration.AspNetCore;
using Glazer.Transactions.Integration.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
                    var Options = NodeOptions.Parse(Args,
                        Preview => Preview.ModuleAssemblies.Add(typeof(Application).Assembly));

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
                            var P2P = Services.AddP2PHostService();

                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureServices(Services, Options);

                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureMvcBuilder(Mvc, Options);

                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureP2PHostService(P2P, Options);

                        })
                        .Configure((Web, App) =>
                        {
                            foreach (var Each in Options.ModuleInstances)
                                Each.ConfigureApplicationBuilder(App, Options);
                        });
                    
                })
                .Build();

            await Host.RunAsync();
        }

    }
}
