using Glazer.Kvdb.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Integration.AspNetCore.Controllers
{
    [Route("api/v1/kvdb/{table}")]
    public class TableController : Controller
    {
        /// <summary>
        /// Test whether
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        public override async Task OnActionExecutionAsync(ActionExecutingContext Context, ActionExecutionDelegate Next)
        {
            var Http = Context.HttpContext;

            if (Http.Request.RouteValues.TryGetValue("table", out var Value) && Value is not null)
            {
                var Name = Value.ToString();
                var Scheme = Http.RequestServices.GetKvScheme();
                var Table = await Scheme.OpenAsync(Name, false, false, Http.RequestAborted);

                if (Table is not null)
                    Http.Items[typeof(IKvTable)] = Table;

                else
                {
                    Context.Result = StatusCode(404);
                    return;
                }
            }

            await base.OnActionExecutionAsync(Context, Next);
        }

        /// <summary>
        /// Token that triggered when the request aborted.
        /// </summary>
        private CancellationToken Token => HttpContext.RequestAborted;

        /// <summary>
        /// Table instance.
        /// </summary>
        public IKvTable Table => HttpContext.Items[typeof(IKvTable)] as IKvTable;

        /// <summary>
        /// Get the value by its key from the table.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [Route("get")][HttpGet]
        public async Task<IActionResult> GetValue([FromQuery(Name = "key")] string Key)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return StatusCode(400);

            var Blob = await Table.GetAsync(Key, Token);
            if (Blob is not null)
                return File(Blob, "application/octet-stream");

            return StatusCode(204); // --> No content.
        }

        private class MultiGetRequest
        {
            [JsonProperty("target_keys")]
            public List<string> Keys { get; set; }
        }

        /// <summary>
        /// Get the multiple values from the table.
        /// </summary>
        /// <returns></returns>
        [Route("get_multi")][HttpPost]
        public async Task<IActionResult> GetValues()
        {
            if (string.IsNullOrWhiteSpace(Request.ContentType))
                return StatusCode(400);

            if (!Request.ContentType.Contains("application/json"))
                return StatusCode(400);

            var DataBytes = await ReadBodyBytes();
            if (DataBytes is null)
                return StatusCode(400);

            MultiGetRequest Targets;
            try { Targets = JsonConvert.DeserializeObject<MultiGetRequest>(Encoding.UTF8.GetString(DataBytes)); }
            catch
            {
                return StatusCode(400);
            }

            if (Targets.Keys is null || Targets.Keys.Count <= 0)
                return StatusCode(400);

            Dictionary<string, string> Base64s = new();
            foreach(var Each in Targets.Keys)
            {
                if (string.IsNullOrWhiteSpace(Each))
                    continue;

                var Value = await Table.GetAsync(Each, Token);
                Base64s[Each] = Value != null ? Convert.ToBase64String(Value) : null;
            }

            return Content(JsonConvert.SerializeObject(Base64s), "application/json", Encoding.UTF8);
        }

        /// <summary>
        /// Set the value by its key into the table.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [Route("set")][HttpPut]
        public async Task<IActionResult> SetValue([FromQuery(Name = "key")] string Key)
        {
            var Options = HttpContext.RequestServices.GetService<KvdbApiOptions>();
            if (Options != null && Options.DisableHttpWrite)
                return StatusCode(404);

            if (Options != null && Options.Authorize != null && !Options.Authorize(HttpContext))
                return StatusCode(401);

            if (string.IsNullOrWhiteSpace(Key))
                return StatusCode(400);

            if (Table.IsReadOnly)
                return StatusCode(403);

            var Data = await ReadBodyBytes();
            if (Data is null)
                return StatusCode(400);

            if (await Table.SetAsync(Key, Data, Token))
                return StatusCode(200);

            return StatusCode(403);
        }

        /// <summary>
        /// Set the multiple values into the table.
        /// </summary>
        /// <returns></returns>
        [Route("set_multi")][HttpPut]
        public async Task<IActionResult> SetValues()
        {
            var Options = HttpContext.RequestServices.GetService<KvdbApiOptions>();
            if (Options != null && Options.DisableHttpWrite)
                return StatusCode(404);

            if (Options != null && Options.Authorize != null && !Options.Authorize(HttpContext))
                return StatusCode(401);

            if (!Request.ContentType.Contains("application/json"))
                return StatusCode(400);
            
            if (Table.IsReadOnly)
                return StatusCode(403);

            var DataBytes = await ReadBodyBytes();
            if (DataBytes is null)
                return StatusCode(400);

            Dictionary<string, string> Base64s;
            try { Base64s = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(DataBytes)); }
            catch
            {
                return StatusCode(400);
            }

            if (Base64s is null || Base64s.Count <= 0)
                return StatusCode(400);

            Dictionary<string, byte[]> Targets = new();
            foreach (var Each in Base64s)
            {
                try     { Targets[Each.Key] = Each.Value != null ? Convert.FromBase64String(Each.Value) : null; }
                catch   { return StatusCode(400); }
            }

            var SucceedKeys = new JArray();
            foreach(var Each in Targets)
            {
                if (await Table.SetAsync(Each.Key, Each.Value, Token))
                    SucceedKeys.Add(Each.Key);
            }

            var Document = new JObject();
            Document["keys"] = SucceedKeys;

            return Content(Document.ToString(Formatting.Indented), "application/json", Encoding.UTF8);
        }

        /// <summary>
        /// Read the body bytes from http context.
        /// </summary>
        /// <returns></returns>
        private async Task<byte[]> ReadBodyBytes()
        {
            var Buffers = new Queue<ArraySegment<byte>>();
            while (true)
            {
                int Length;
                var Buffer = new byte[4096];

                try { Length = await HttpContext.Request.Body.ReadAsync(Buffer, Token); }
                catch (EndOfStreamException) { Length = 0; }
                catch
                {
                    return null;
                }

                if (Length <= 0)
                    break;

                Buffers.Enqueue(new ArraySegment<byte>(Buffer, 0, Length));
            }

            var Data = new byte[Buffers.Sum(X => X.Count)];
            var Offset = 0;

            while (Buffers.TryDequeue(out var Buffer))
            {
                Buffer.CopyTo(Data, Offset);
                Offset += Buffer.Count;
            }

            return Data;
        }
    }
}
