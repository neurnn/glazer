using Backrole.Core.Abstractions;
using Glazer.Storages.Abstractions;
using System;

namespace Glazer.Storages.Server.Abstractions
{
    public interface IBlobStorageServiceCollection : IServiceCollection
    {
        /// <summary>
        /// Add the storage using the name.
        /// </summary>
        /// <param name="Name">null or empty as default.</param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        IBlobStorageServiceCollection AddStorage(string Name, Func<IServiceProvider, IBlobStorage> Factory);
    }
}
