using Glazer.Storages.Abstractions;

namespace Glazer.Storages.Server.Abstractions
{
    public interface IBlobStorageProvider
    {
        /// <summary>
        /// Get Storage instance using its name.
        /// </summary>
        /// <param name="Name">null or empty as default.</param>
        /// <returns></returns>
        IBlobStorage GetStorage(string Name = null);
    }
}
