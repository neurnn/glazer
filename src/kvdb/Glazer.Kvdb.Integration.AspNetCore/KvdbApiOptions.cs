using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Integration.AspNetCore
{
    public class KvdbApiOptions
    {
        /// <summary>
        /// Disable the HTTP writing APIs.
        /// </summary>
        public bool DisableHttpWrite { get; set; } = true;

        /// <summary>
        /// Authorization Callback to allow writing APIs.
        /// If set, the writing APIs will check authorization using this callback.
        /// And <see cref="DisableHttpWrite"/> set true, entire writing APIs are disabled.
        /// </summary>
        public Func<HttpContext, bool> Authorize { get; set; }
    }
}
