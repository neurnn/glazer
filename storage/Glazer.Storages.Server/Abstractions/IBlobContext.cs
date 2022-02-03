using Backrole.Core.Abstractions;
using Backrole.Http.Abstractions;
using Glazer.Storages.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Glazer.Storages.Server.Abstractions
{
    public interface IBlobContext
    {
        /// <summary>
        /// Http Context instance.
        /// </summary>
        IHttpContext HttpContext { get; }

        /// <summary>
        /// A central location to share objects between middlewares.
        /// </summary>
        IServiceProperties Properties { get; }

        /// <summary>
        /// Storage Provider that is to used to find the storage by its name.
        /// </summary>
        IBlobStorageProvider Storages { get; }

        /// <summary>
        /// Indicates whether the request is blob request or not.
        /// </summary>
        bool IsBlobRequest { get; }

        /// <summary>
        /// Method of the request.
        /// </summary>
        BlobMethod Method { get; set; }

        /// <summary>
        /// Key to access.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// ETag to match.
        /// </summary>
        string ETag { get; set; }

        /// <summary>
        /// Options.
        /// </summary>
        object Options { get; set; }

        /// <summary>
        /// Result to send to the remote host.
        /// </summary>
        IBlobResult Result { get; set; }

        /// <summary>
        /// Lock result to send to the remote host.
        /// </summary>
        IBlobLockResult LockResult { get; set; }

        /// <summary>
        /// Get the request content if available.
        /// </summary>
        /// <returns></returns>
        byte[] GetRequestContent();

        /// <summary>
        /// Get the request content asynchronously if available.
        /// </summary>
        /// <returns></returns>
        Task<byte[]> GetRequestContentAsync();
    }
}
