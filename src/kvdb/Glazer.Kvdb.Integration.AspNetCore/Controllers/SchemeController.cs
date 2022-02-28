using Glazer.Kvdb.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Integration.AspNetCore.Controllers
{
    [Route("api/v1/kvdb")]
    public class SchemeController : Controller
    {
        /// <summary>
        /// Token that triggered when the request aborted.
        /// </summary>
        private CancellationToken Token => HttpContext.RequestAborted;

        /// <summary>
        /// List all tables asynchronously.
        /// Usage: /api/v1/kvdb/scheme/tables (GET)
        /// </summary>
        /// <returns></returns>
        [Route("tables")][HttpGet]
        public async Task<IActionResult> ListAsync()
        {
            var Scheme = HttpContext.RequestServices.GetKvScheme();
            var Document = new JObject();
            {
                var Result = new JArray();
                await foreach (var Each in Scheme.ListAsync(Token))
                {
                    Result.Add(Each);
                }

                Document["tables"] = Result;
            }

            return Content(Document.ToString(Formatting.Indented), "application/json", Encoding.UTF8);
        }

        /// <summary>
        /// Create a table asynchronosuly.
        /// Usage: /api/v1/kvdb/scheme/tables (POST)
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        [Route("tables")][HttpPost]
        public async Task<IActionResult> CreateAsync([FromQuery(Name = "name")] string Name)
        {
            var Options = HttpContext.RequestServices.GetService<KvdbApiOptions>();
            if (Options != null && Options.DisableHttpWrite)
                return StatusCode(404);

            if (Options != null && Options.Authorize != null && !Options.Authorize(HttpContext))
                return StatusCode(401);

            var Scheme = HttpContext.RequestServices.GetKvScheme();
            if (string.IsNullOrWhiteSpace(Name))
                return StatusCode(400);

            using var Table = await Scheme.CreateAsync(Name, Token);
            if (Table is null)
                return StatusCode(403);

            return StatusCode(200);
        }
        
        /// <summary>
        /// Drop a table asynchronosuly.
        /// Usage: /api/v1/kvdb/scheme/tables (DELETE)
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        [Route("tables")][HttpDelete]
        public async Task<IActionResult> DropAsync([FromQuery(Name = "name")] string Name)
        {
            var Options = HttpContext.RequestServices.GetService<KvdbApiOptions>();
            if (Options != null && Options.DisableHttpWrite)
                return StatusCode(404);

            if (Options != null && Options.Authorize != null && !Options.Authorize(HttpContext))
                return StatusCode(401);

            var Scheme = HttpContext.RequestServices.GetKvScheme();
            if (string.IsNullOrWhiteSpace(Name))
                return StatusCode(400);

            if (await Scheme.DropAsync(Name, Token))
                return StatusCode(200);

            return StatusCode(403);
        }
    }
}
