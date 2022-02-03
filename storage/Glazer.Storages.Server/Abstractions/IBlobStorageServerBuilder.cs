using Backrole.Core.Abstractions;
using Backrole.Http.Abstractions;
using Glazer.Storages.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storages.Server.Abstractions
{
    public interface IBlobStorageServerBuilder
    {
        /// <summary>
        /// A central location to share objects between server components.
        /// </summary>
        IServiceProperties Properties { get; }

        /// <summary>
        /// Adds a delegate that configures the storage services.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        IBlobStorageServerBuilder ConfigureServices(Action<IBlobStorageServiceCollection> Delegate);

        /// <summary>
        /// Adds a delegate that configure registered services.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        IBlobStorageServerBuilder Configure(Action<IConfiguration, IServiceProvider> Delegate);

        /// <summary>
        /// Adds a middleware that handles the blob storage requests.
        /// </summary>
        /// <param name="Middleware"></param>
        /// <returns></returns>
        IBlobStorageServerBuilder Use(Func<IBlobContext, Func<Task>, Task> Middleware);
    }
}
