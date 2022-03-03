using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common.Internals.Engine;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Get the <see cref="INodeEngineFactory"/> instance from <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="Services"></param>
        /// <returns></returns>
        internal static INodeEngineFactory GetNodeEngineFactory(this IServiceCollection Services)
        {
            var Descriptor = Services.FirstOrDefault(X => X.ServiceType == typeof(INodeEngineFactory));
            if (Descriptor is null)
            {
                /* Set the engine factory if it isn't configured yet.*/
                Services.AddSingleton<INodeEngineFactory>(new NodeEngineFactory());
                return GetNodeEngineFactory(Services);
            }

            return Descriptor.ImplementationInstance as INodeEngineFactory;
        }

        /// <summary>
        /// Set the node engine factory that is used for the specified mode.
        /// </summary>
        /// <param name="Mode"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public static IServiceCollection SetNodeEngine(this IServiceCollection Services, NodeMode Mode, Func<IServiceProvider, INodeEngine> Factory)
        {
            Services.GetNodeEngineFactory().Set(Mode, Factory);
            return Services;
        }
    }
}
