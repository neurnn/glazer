using Glazer.Storage.Abstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glazer.Storage.Integration.AspNetCore
{
    public static class StorageExtensions
    {
        /// <summary>
        /// Set the block storage to service collection.
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public static IServiceCollection SetBlockStorage(this IServiceCollection Services, Func<IServiceProvider, IStorage> Factory)
            => Services.AddSingleton(Services => Factory(Services));

        /// <summary>
        /// Get the block storage instance.
        /// </summary>
        /// <param name="Services"></param>
        /// <returns></returns>
        public static IStorage GetBlockStorage(this IServiceProvider Services) => Services.GetRequiredService<IStorage>();

        /// <summary>
        /// Add Block Storage API sets to MVC builder.
        /// </summary>
        /// <param name="MvcBuilder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddBlockStorageApiControllers(this IMvcBuilder MvcBuilder)
        {
            return MvcBuilder.AddApplicationPart(typeof(StorageExtensions).Assembly);
        }
    }
}
