using Glazer.Kvdb.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Glazer.Kvdb.Integration.AspNetCore
{
    public static class KvdbExtensions
    {
        /// <summary>
        /// Add the KV scheme to service collection.
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public static IServiceCollection SetKvScheme(this IServiceCollection Services, Func<IServiceProvider, IKvScheme> Factory) 
            => Services.AddSingleton(Services => Factory(Services));

        /// <summary>
        /// Get the KV scheme from <see cref="IServiceProvider"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        /// <returns></returns>
        public static IKvScheme GetKvScheme(this IServiceProvider Services) => Services.GetRequiredService<IKvScheme>();

        /// <summary>
        /// Adds a delegate that configures <see cref="KvdbApiOptions"/> instance that is for serving HTTP APIs.
        /// </summary>
        /// <param name="HostBuilder"></param>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        public static IWebHostBuilder ConfigureKvdbApiOptions(this IWebHostBuilder HostBuilder, Action<KvdbApiOptions> Delegate)
        {
            return HostBuilder
                .ConfigureServices(Services =>
                {
                    var Registration = Services.FirstOrDefault(X => X.ServiceType == typeof(KvdbApiOptions));
                    if (Registration is null || Registration.ImplementationInstance is not KvdbApiOptions Options)
                        Services.AddSingleton(Options = new KvdbApiOptions());

                    Delegate?.Invoke(Options);
                });
        }

        /// <summary>
        /// Add Kvdb API controllers to the MVC builder.
        /// </summary>
        /// <param name="MvcBuilder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddKvdbApiControllers(this IMvcBuilder MvcBuilder)
        {
            return MvcBuilder.AddApplicationPart(typeof(KvdbExtensions).Assembly);
        }
    }
}
