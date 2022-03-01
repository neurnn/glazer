using Glazer.Nodes.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Glazer.Nodes.Modules
{
    public sealed class PostInit : NodeModule<PostInit>
    {
        /// <inheritdoc/>
        public override int Priority => int.MaxValue;

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection Services, NodeOptions Options)
        {
            Services
                .AddSingleton(Options);
        }

        /// <inheritdoc/>
        public override void ConfigureApplicationBuilder(IApplicationBuilder Http, NodeOptions Options)
        {
            Http
                .UseEndpoints(Endpoints =>
                {
                    Endpoints.MapControllers();
                });
        }
    }
}
