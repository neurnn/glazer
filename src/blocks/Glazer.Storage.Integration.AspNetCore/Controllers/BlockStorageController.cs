using Glazer.Common;
using Glazer.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storage.Integration.AspNetCore.Controllers
{
    [Route("api/v1/block")]
    public class BlockStorageController : Controller
    {
        /// <summary>
        /// Get the block storage state asynchronously.
        /// Usage: /api/v1/block/state
        /// </summary>
        /// <returns></returns>
        [Route("state")][HttpGet]
        public IActionResult GetState()
        {
            var Storage = HttpContext.RequestServices.GetBlockStorage();
            var Json = new JObject();

            Json["initial_block_id"] = Storage.InitialBlockId.ToString();
            Json["latest_block_id"] = Storage.LatestBlockId.ToString();

            return Content(Json.ToString(Formatting.Indented), "application/json", Encoding.UTF8);
        }

        /// <summary>
        /// Get the block asynchronously.
        /// Usage: /api/v1/block/get?id={BLOCK_ID}
        /// </summary>
        /// <param name="BlockId"></param>
        /// <returns></returns>
        [Route("get")][HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery(Name = "id")] Guid BlockId)
        {
            if (BlockId == Guid.Empty)
                return StatusCode(400);

            var Storage = HttpContext.RequestServices.GetBlockStorage();
            var Block = await Storage.GetAsync(new BlockId(BlockId), HttpContext.RequestAborted);

            if (!Block.IsValid)
            {
                return StatusCode(404);
            }

            var Json = new JObject();
            Json["id"] = BlockId.ToString();

            if (!Block.TryExport(Json, Block))
                return StatusCode(404);

            return Content(Json.ToString(Formatting.Indented), "application/json", Encoding.UTF8);
        }

        /// <summary>
        /// Get the surface value of the blockchain asynchronously.
        /// Usage: /api/v1/block/get_value?key=...
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [Route("get_value")][HttpGet]
        public async Task<IActionResult> GetValueAsync([FromQuery(Name = "key")] string Key)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return StatusCode(400);

            var Storage = HttpContext.RequestServices.GetBlockStorage();
            var Value = await Storage.SurfaceSet.GetAsync(Key, HttpContext.RequestAborted);

            if (Value is null)
                return StatusCode(204);

            var Json = BsonConvert.Deserialize<JObject>(Value);
            return Content(Json.ToString(Formatting.Indented), "application/json", Encoding.UTF8);
        }
    }
}
