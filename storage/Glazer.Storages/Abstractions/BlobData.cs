using System;

namespace Glazer.Storages.Abstractions
{
    /// <summary>
    /// Data of the blob.
    /// </summary>
    public struct BlobData
    {
        /// <summary>
        /// Etag of the entity. (Set if required)
        /// If Etag set, the storage will update the blob only if same.
        /// </summary>
        public string Etag { get; set; }

        /// <summary>
        /// Data.
        /// </summary>
        public ArraySegment<byte> Data { get; set; }
    }
}
