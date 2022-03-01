using Glazer.P2P.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Glazer.Nodes.Abstractions
{
    public abstract class NodeModule
    {
        /// <summary>
        /// Hides the constructor to prevent the module is derived from this non-generic class.
        /// </summary>
        internal NodeModule()
        {

        }

        /// <summary>
        /// Priority of the Module.
        /// </summary>
        public virtual int Priority { get; } = 0;

        /// <summary>
        /// Prerequisite Module Types.
        /// </summary>
        public virtual Type[] Dependencies { get; } = Type.EmptyTypes;

        /// <summary>
        /// Capture module options for the <see cref="NodeOptions.ModuleExtras"/> from the <paramref name="Arguments"/>.
        /// </summary>
        /// <param name="Arguments"></param>
        /// <param name="Options"></param>
        public virtual void CaptureOptions(Queue<string> Arguments, Queue<string> Remainders, NodeOptions Options) { }

        /// <summary>
        /// Configure the <see cref="ILoggingBuilder"/> with <see cref="NodeOptions"/> instance.
        /// </summary>
        /// <param name="Logging"></param>
        /// <param name="Options"></param>
        public virtual void ConfigureLogging(ILoggingBuilder Logging, NodeOptions Options) { }

        /// <summary>
        /// Configure the <see cref="IWebHostBuilder"/> with <see cref="NodeOptions"/> instance.
        /// </summary>
        /// <param name="Host"></param>
        /// <param name="Options"></param>
        public virtual void ConfigureWebHostBuilder(IWebHostBuilder Host, NodeOptions Options) { }

        /// <summary>
        /// Configure the <see cref="IServiceCollection"/> with <see cref="NodeOptions"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        public virtual void ConfigureServices(IServiceCollection Services, NodeOptions Options) { }

        /// <summary>
        /// Configure the <see cref="IMvcBuilder"/> with <see cref="NodeOptions"/> instance.
        /// </summary>
        /// <param name="Mvc"></param>
        /// <param name="Options"></param>
        public virtual void ConfigureMvcBuilder(IMvcBuilder Mvc, NodeOptions Options) { }

        /// <summary>
        /// Configure the <see cref="IMessangerHostBuilder"/> with <see cref="NodeOptions"/> instance.
        /// </summary>
        /// <param name="P2P"></param>
        /// <param name="Options"></param>
        public virtual void ConfigureP2PHostService(IMessangerHostBuilder P2P, NodeOptions Options) { }

        /// <summary>
        /// Configure the <see cref="IApplicationBuilder"/> with <see cref="NodeOptions"/> instance.
        /// </summary>
        /// <param name="Http"></param>
        /// <param name="Options"></param>
        public virtual void ConfigureApplicationBuilder(IApplicationBuilder Http, NodeOptions Options) { }
    }

    public abstract class NodeModule<TSelf> : NodeModule where TSelf : NodeModule<TSelf>, new()
    {
        /// <summary>
        /// Initialize a new <see cref="TSelf"/> instance.
        /// </summary>
        public NodeModule()
        {
        }

        /// <summary>
        /// Type of <see cref="TSelf"/> module to refer as <see cref="NodeModule.Dependencies"/>.
        /// </summary>
        public static readonly Type Type = typeof(TSelf);

    }
}
