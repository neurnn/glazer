using Backrole.Core.Abstractions;
using Backrole.Http.Abstractions;
using Glazer.Storages.Abstractions;
using Glazer.Storages.Internals.Http;
using Glazer.Storages.Server.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storages.Server.Internals
{
    internal class BlobContext : IBlobContext
    {
        private static readonly byte[] EMPTY_BYTES = new byte[0];

        private BlobStorageOptions m_Options;
        private byte[] m_CachedData;
        private bool m_Handled = false;

        /// <summary>
        /// Initialize a new <see cref="BlobContext"/> instance.
        /// </summary>
        /// <param name="HttpContext"></param>
        /// <param name="Storages"></param>
        /// <param name="Options"></param>
        public BlobContext(IHttpContext HttpContext, IBlobStorageProvider Storages, IOptions<BlobStorageOptions> Options)
        {
            this.HttpContext = HttpContext;
            this.Storages = Storages;
            m_Options = Options.Value;

            var Request = HttpContext.Request;
            var PathString = StorageHelpers.Normalize(Request.PathString);

            if (PathString.StartsWith($"{m_Options.MapTo}/"))
                PathString = PathString.Substring(m_Options.MapTo.Length + 1).TrimStart();

            else
            {
                IsBlobRequest = false;
                return;
            }

            ParseRequest(Request, PathString);
        }

        /// <summary>
        /// Parse the request.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="PathString"></param>
        private void ParseRequest(IHttpRequest Request, string PathString)
        {
            if (PathString == "test")
            {
                Method = BlobMethod.Test;
                return;
            }

            try
            {
                if (PathString == "lock")
                {
                    if (Request.Method.ToUpper() != "POST")
                    {
                        Method = BlobMethod.None;
                        return;
                    }

                    var Body = JsonConvert.DeserializeObject<LockRequest>(GetRequestText());
                    Method = BlobMethod.Lock; Key = Body.Key; Options = Body;
                    return;
                }

                if (PathString == "unlock")
                {
                    if (Request.Method.ToUpper() != "POST")
                    {
                        Method = BlobMethod.None;
                        return;
                    }

                    var Body = JsonConvert.DeserializeObject<UnlockRequest>(GetRequestText());
                    Method = BlobMethod.Unlock; Key = Body.Key; Options = Body;
                    return;
                }
            }
            catch
            {
                IsBlobRequest = false;
                return;
            }
            
            if (PathString.StartsWith("blob/"))
            {
                switch (Request.Method.ToUpper())
                {
                    case "GET": Method = BlobMethod.Read; break;
                    case "POST": Method = BlobMethod.Create; break;
                    case "PUT": Method = BlobMethod.Write; break;
                    case "DELETE": Method = BlobMethod.Remove; break;
                    default:
                        Method = BlobMethod.None;
                        IsBlobRequest = false;
                        return;
                }

                Key = PathString.Substring(5);
            }

            if ((ETag = Request.Headers.GetValue("If-Match", "").Trim(' ', '\t', '"', '\'')).Length <= 0)
                 ETag = null;
        }

        /// <inheritdoc/>
        public IHttpContext HttpContext { get; }

        /// <inheritdoc/>
        public IServiceProperties Properties => HttpContext.Properties;

        /// <inheritdoc/>
        public IBlobStorageProvider Storages { get; }

        /// <inheritdoc/>
        public BlobMethod Method { get; set; } = BlobMethod.None;

        /// <inheritdoc/>
        public bool IsBlobRequest { get; set; } = true;

        /// <inheritdoc/>
        public string Key { get; set; } = null;

        /// <inheritdoc/>
        public string ETag { get; set; }

        /// <inheritdoc/>
        public object Options { get; set; }

        /// <inheritdoc/>
        public IBlobResult Result { get; set; }

        /// <inheritdoc/>
        public IBlobLockResult LockResult { get; set; }

        /// <summary>
        /// Get Request Content as Text.
        /// </summary>
        /// <returns></returns>
        private string GetRequestText() => Encoding.UTF8.GetString(GetRequestContent());

        /// <inheritdoc/>
        public byte[] GetRequestContent() => GetRequestContentAsync().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public async Task<byte[]> GetRequestContentAsync()
        {
            if (m_CachedData is null)
            {
                var Bytes = new Queue<byte[]>();
                await ReceiveBytesToList(Bytes);

                /* Merge all byte slices. */
                if (Bytes.Count <= 0)
                    m_CachedData = EMPTY_BYTES;

                else
                {
                    var RetVal = Bytes.Dequeue();
                    while (Bytes.TryDequeue(out var Each))
                    {
                        var Offset = RetVal.Length;

                        Array.Resize(ref RetVal, Offset + Each.Length);
                        Buffer.BlockCopy(Each, 0, RetVal, Offset, Each.Length);
                    }

                    m_CachedData = RetVal;
                }
            }

            return m_CachedData;
        }

        private async Task ReceiveBytesToList(Queue<byte[]> Bytes)
        {
            var Buffer = new ArraySegment<byte>(new byte[4096]);

            while (true)
            {
                int Length;
                if (Buffer.Count <= 0)
                {
                    Buffer = new ArraySegment<byte>(Buffer.Array, 0, Buffer.Offset);
                    Bytes.Enqueue(Buffer.ToArray());
                }

                try { Length = await HttpContext.Request.InputStream.ReadAsync(Buffer); }
                catch
                {
                    Bytes.Clear();
                    return;
                }

                if (Length <= 0)
                {
                    if (Buffer.Offset > 0)
                    {
                        Buffer = new ArraySegment<byte>(Buffer.Array, 0, Buffer.Offset);
                        Bytes.Enqueue(Buffer.ToArray());
                    }

                    break;
                }

                Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset + Length, Buffer.Count - Length);
            }
        }

        /// <summary>
        /// Send the blob context to the remote client.
        /// </summary>
        /// <param name="Http"></param>
        /// <returns></returns>
        public async Task SendAsync(IHttpContext Http)
        {
            lock(this)
            {
                if (m_Handled)
                    return;

                m_Handled = true;
            }

            try
            {
                switch (Method)
                {
                    case BlobMethod.Lock:
                        SetLockResponse(Http);
                        break;

                    case BlobMethod.Unlock:
                        await SetUnlockResponse(Http);
                        break;

                    case BlobMethod.Create:
                    case BlobMethod.Read:
                    case BlobMethod.Write:
                    case BlobMethod.Remove:
                        await SetDataResult(Http);
                        break;

                    default:
                        Http.Response.Status = 400;
                        Http.Response.StatusPhrase = "Bad Request";
                        return;
                }
            }
            finally
            {
                try
                {
                    if (Result != null)
                        await Result.DisposeAsync();
                }
                catch { }
            }
        }

        /// <summary>
        /// Set Data Result.
        /// </summary>
        /// <param name="Http"></param>
        /// <returns></returns>
        private async Task SetDataResult(IHttpContext Http)
        {
            if (Result is null)
            {
                Http.Response.Status = 404;
                Http.Response.StatusPhrase = "Not Found";
                return;
            }

            Http.Response.Status = (int)Result.Status;
            Http.Response.StatusPhrase = null;

            if (Result.CreationTime != DateTime.MinValue)
            {
                var RTime = Result.CreationTime;
                if (RTime.Kind != DateTimeKind.Utc)
                    RTime = RTime.ToUniversalTime();

                Http.Response.Headers.Set("X-Glazer-BlobCTime",
                    (RTime - DateTime.UnixEpoch).TotalSeconds.ToString());
            }

            if (Result.ModifiedTime != DateTime.MinValue)
            {
                var RTime = Result.ModifiedTime;
                if (RTime.Kind != DateTimeKind.Utc)
                    RTime = RTime.ToUniversalTime();

                Http.Response.Headers.Set("X-Glazer-BlobMTime",
                    (RTime - DateTime.UnixEpoch).TotalSeconds.ToString());
            }

            if (Result.Status != BlobStatus.Ok)
                return;

            try
            {
                if (!string.IsNullOrWhiteSpace(Result.ETag))
                    Http.Response.Headers.Set("ETag", $"\"{Result.ETag}\"");

                if (Method == BlobMethod.Read)
                {
                    var Blob = await Result.ReadAsync(Http.Aborted);
                    Http.Response.OutputStream = new MemoryStream(
                        Blob.Data.Array, Blob.Data.Offset, Blob.Data.Count, false);
                }
            }
            catch
            {
                Http.Response.Status = 500;
                Http.Response.StatusPhrase = "Internal Server Error";
            }
        }

        /// <summary>
        /// Set the lock response to <see cref="IHttpContext"/>.
        /// </summary>
        /// <param name="Http"></param>
        /// <returns></returns>
        private void SetLockResponse(IHttpContext Http)
        {
            if (LockResult is null)
            {
                Http.Response.Status = 404;
                Http.Response.StatusPhrase = "Not Found";
                return;
            }

            if (LockResult.Status == BlobStatus.Ok)
            {
                var Locks = Http.Services.GetRequiredService<BlobLockManager>();
                var Guid = Locks.Register(LockResult);

                var LockTime = LockResult.LockedTime;
                if (LockTime.Kind != DateTimeKind.Utc)
                    LockTime = LockTime.ToUniversalTime();

                Http.Response.Headers.Set("Content-Type", "application/json; charset=utf-8");
                Http.Response.OutputStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                    new LockResponse
                    {
                        Token = Guid.ToString(),
                        LockedTime = (LockTime - DateTime.UnixEpoch).TotalSeconds,
                        Expiration = LockResult.Expiration.TotalSeconds
                    })));
            }
            
            else
            {
                Http.Response.Status = (int)LockResult.Status;
                Http.Response.StatusPhrase = null;
            }
        }

        /// <summary>
        /// Set the unlock response to <see cref="IHttpContext"/>.
        /// </summary>
        /// <param name="Http"></param>
        /// <returns></returns>
        private async Task SetUnlockResponse(IHttpContext Http)
        {
            if (!(Options is UnlockRequest Unlock) || !Guid.TryParse(Unlock.Token, out var Target))
                Http.Response.Status = 400;

            else
            {
                var Locks = Http.Services.GetRequiredService<BlobLockManager>();
                if (!Locks.Unregister(Target, out var Lock))
                {
                    Http.Response.Status = 404;
                    Http.Response.StatusPhrase = "Not Found";
                    return;
                }

                Http.Response.Status = 200;
                Http.Response.StatusPhrase = "OK";

                try { await Lock.DisposeAsync(); }
                catch
                {

                }
            }
        }
    }
}
