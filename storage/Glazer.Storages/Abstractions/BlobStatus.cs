
namespace Glazer.Storages.Abstractions
{
    /// <summary>
    /// Blob Operation Status.
    /// </summary>
    public enum BlobStatus
    {
        /// <summary>
        /// Success.
        /// </summary>
        Ok = 200,

        /// <summary>
        /// Bad Request.
        /// </summary>
        BadRequest = 400,

        /// <summary>
        /// Unauthorized.
        /// </summary>
        Unauthorized = 401,

        /// <summary>
        /// Forbidden.
        /// </summary>
        Forbidden = 403,

        /// <summary>
        /// Not Found.
        /// </summary>
        NotFound = 404,

        /// <summary>
        /// Conflict.
        /// </summary>
        Conflict = 409,

        /// <summary>
        /// Timeout reached.
        /// </summary>
        Timedout = 408,

        /// <summary>
        /// Precondition failed.
        /// </summary>
        PreconditionFailed = 412,

        /// <summary>
        /// Canceled.
        /// </summary>
        Canceled = 425,

        /// <summary>
        /// Storage Error.
        /// </summary>
        StorageError = 500,

        /// <summary>
        /// Service Unavailable.
        /// </summary>
        Unavailable = 503
    }
}
