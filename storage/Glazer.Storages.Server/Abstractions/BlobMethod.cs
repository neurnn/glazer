namespace Glazer.Storages.Server.Abstractions
{
    public enum BlobMethod
    {
        /// <summary>
        /// Not a blob request.
        /// </summary>
        None,
        
        /// <summary>
        /// Test endpoint.
        /// </summary>
        Test,

        /// <summary>
        /// Lock a blob.
        /// </summary>
        Lock,

        /// <summary>
        /// Unlock a blob.
        /// </summary>
        Unlock,

        /// <summary>
        /// Create a blob.
        /// </summary>
        Create,

        /// <summary>
        /// Get a blob.
        /// </summary>
        Read,

        /// <summary>
        /// Update a blob.
        /// </summary>
        Write,

        /// <summary>
        /// Remove a blob.
        /// </summary>
        Remove
    }
}
