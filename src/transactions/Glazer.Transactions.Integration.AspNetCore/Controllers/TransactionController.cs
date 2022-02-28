using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Models;
using Glazer.Transactions.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Transactions.Integration.AspNetCore.Controllers
{
    [Route("api/v1/transactions")]
    public class TransactionController : Controller
    {
        /// <summary>
        /// Transaction Sets.
        /// </summary>
        private ITransactionSets Transactions => HttpContext.RequestServices.GetTransactionSets();

        /// <summary>
        /// Token that triggered when the request aborted.
        /// </summary>
        private CancellationToken Token => HttpContext.RequestAborted;

        /// <summary>
        /// Response abount <see cref="TransactionRequest"/>.
        /// </summary>
        private struct RequestResponse
        {
            [JsonProperty("trx_id")]
            public string Id { get; set; }

            [JsonProperty("state")]
            public string State { get; set; }

            [JsonProperty("reason")]
            public string Reason { get; set; }

            /// <summary>
            /// Serialize to JSON.
            /// </summary>
            /// <returns></returns>
            public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Make <see cref="RequestResponse"/> from <see cref="TransactionRegistration"/>.
        /// </summary>
        /// <param name="Status"></param>
        /// <returns></returns>
        private RequestResponse RegistrationToReply(TransactionRegistration Status) => new RequestResponse
        {
            Id = Status.Id.IsValid ? Status.Id.ToString() : null,
            State = Status.Status.ToString().ToLower(),
            Reason = string.Empty
        };

        /// <summary>
        /// Make <see cref="RequestResponse"/> from <see cref="TransactionExecutionStatus"/>.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        private RequestResponse ExecutionStatusToReply(string Id, TransactionExecutionStatus Status) => new RequestResponse
        {
            Id = Id,
            State = Status.Status.ToString().ToLower(),
            Reason = Status.Reason
        };

        /// <summary>
        /// Encode the <see cref="RequestResponse"/> to <see cref="ContentResult"/> that contains JSON reply.
        /// </summary>
        /// <param name="Reply"></param>
        /// <returns></returns>
        private ContentResult ToContentResult(RequestResponse Reply) => Content(Reply.ToJson(), "application/json", Encoding.UTF8);

        /// <summary>
        /// Enqueue the transaction request.
        /// </summary>
        /// <returns></returns>
        [Route("enqueue")][HttpPost]
        public async Task<IActionResult> Enqueue()
        {
            var ContentType = GetContentType();
            if (ContentType.Length <= 0)
                return StatusCode(400);

            var Request = new TransactionRequest();
            var Body = await ReadBodyBytes();
            if (Body is null || Body.Length <= 0)
                return StatusCode(400);

            var Reply = new RequestResponse
            {
                Id = null,
                State = "queueerror",
                Reason = "no request body interpreted"
            };

            if (ContentType.Equals("application/x-glazer-trx", StringComparison.OrdinalIgnoreCase))
            {
                using var Reader = new PacketReader(Body);

                try { Request.Import(Reader); }
                catch(Exception e)
                {
                    Reply.State = "queueerror";
                    Reply.Reason = e.Message;
                    return ToContentResult(Reply);
                }

                Reply = RegistrationToReply(Transactions.PendingSet.Enqueue(Request));
            }

            else if (ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                var Json = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(Body));

                try { Request.Import(Json); }
                catch (Exception e)
                {
                    Reply.State = "queueerror";
                    Reply.Reason = e.Message;
                    return ToContentResult(Reply);
                }

                Reply = RegistrationToReply(Transactions.PendingSet.Enqueue(Request));
            }

            return ToContentResult(Reply);
        }

        /// <summary>
        /// Get the status of the transaction.
        /// </summary>
        /// <param name="TrxId"></param>
        /// <returns></returns>
        [Route("get_status")][HttpGet]
        public IActionResult GetStatus([FromQuery(Name = "trx_id")] string TrxId)
        {
            if (string.IsNullOrWhiteSpace(TrxId) || !HashValue.TryParse(TrxId, out var Id))
                return StatusCode(400);

            var Status = Transactions.PeekExecutionStatus(Id);
            return ToContentResult(ExecutionStatusToReply(TrxId, Status));
        }

        /// <summary>
        /// Get the pending requests.
        /// </summary>
        /// <returns></returns>
        [Route("get_pendings")][HttpGet]
        public IActionResult GetPendings()
        {
            var Pendings = new List<TransactionRegistration>();
            Transactions.PendingSet.GetPendings(Pendings);

            var Document = new JObject();
            foreach(var Each in Pendings)
            {
                if (Each.Status != TransactionStatus.Queued)
                    continue;

                var Json = new JObject();

                try     { Each.Request.Export(Json); }
                catch   { continue; }

                Document[Each.Id] = Json;
            }

            return Content(Document.ToString(Formatting.Indented), "application/json", Encoding.UTF8);
        }
        
        /// <summary>
        /// Get the working requests.
        /// </summary>
        /// <returns></returns>
        [Route("get_workings")][HttpGet]
        public IActionResult GetWorkings()
        {
            var Workings = new List<TransactionRegistration>();
            Transactions.WorkingSet.GetPendings(Workings);

            var Document = new JObject();
            foreach (var Each in Workings)
            {
                if (Each.Status != TransactionStatus.Queued)
                    continue;

                var Json = new JObject();

                try { Each.Request.Export(Json); }
                catch { continue; }

                Document[Each.Id] = Json;
            }

            return Content(Document.ToString(Formatting.Indented), "application/json", Encoding.UTF8);
        }

        /// <summary>
        /// Get the content type.
        /// </summary>
        /// <returns></returns>
        private string GetContentType()
        {
            if (HttpContext.Request.Headers.TryGetValue("Content-Type", out var Value))
                return Value.FirstOrDefault();

            return string.Empty;
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
