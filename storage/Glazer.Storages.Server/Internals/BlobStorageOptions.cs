using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storages.Server.Internals
{
    internal class BlobStorageOptions
    {
        /// <summary>
        /// Map the storage API to the path.
        /// </summary>
        public string MapTo { get; set; } = "api/v1";
    }
}
