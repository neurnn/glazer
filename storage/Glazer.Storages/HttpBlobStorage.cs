using Glazer.Storages.Abstractions;
using Glazer.Storages.Internals.Http;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages
{
    /// <summary>
    /// Blob Storage Client that uses Http as transport.
    /// </summary>
    public class HttpBlobStorage : IBlobStorage
    {
        private HttpClient m_Http = new();
        private string m_BaseAddress;

        /// <summary>
        /// Initialize a new <see cref="HttpBlobStorage"/> instance.
        /// </summary>
        /// <param name="BaseAddress"></param>
        /// <param name="Configure"></param>
        public HttpBlobStorage(Uri BaseAddress, Action<HttpClient> Configure = null)
        {
            m_BaseAddress = BaseAddress.ToString().TrimEnd('/');
            Configure?.Invoke(m_Http);

            var AssemblyName = typeof(HttpBlobStorage).Assembly.GetName();
            m_Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
                AssemblyName.Name, AssemblyName.Version.ToString()));
        }

        /// <summary>
        /// Indicates whether the storage is local or not.
        /// </summary>
        public bool IsLocalStorage { get; } = false;

        /// <summary>
        /// Send the request.
        /// </summary>
        /// <param name="Method"></param>
        /// <param name="Path"></param>
        /// <param name="Content"></param>
        /// <param name="Headers"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> RequestAsync(HttpMethod Method, string Path, HttpContent Content = null, Action<HttpRequestHeaders> Headers = null, CancellationToken Token = default)
        {
            using (var Request = new HttpRequestMessage(Method, string.Join('/', m_BaseAddress, Path)))
            {
                Headers?.Invoke(Request.Headers);

                if (Content != null)
                    Request.Content = Content;

                await OnRequestAsync(Request, Token);
                return await m_Http.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, Token);
            }
        }

        /// <summary>
        /// Called to manipulate the request if required.
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        protected virtual Task OnRequestAsync(HttpRequestMessage Request, CancellationToken Token) => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task<BlobStatus> TestAsync(CancellationToken Token = default)
        {
            try
            {
                using (var Http = await RequestAsync(HttpMethod.Get, "test", null, null, Token))
                {
                    if (Http.IsSuccessStatusCode)
                        return BlobStatus.Ok;

                    return (BlobStatus)Http.StatusCode;
                }
            }

            catch (OperationCanceledException) { return BlobStatus.Canceled; }
            catch (HttpRequestException Ex)
            {
                if (Ex.StatusCode.HasValue)
                    return (BlobStatus)Ex.StatusCode.Value;
            }

            return BlobStatus.Timedout;
        }

        /// <inheritdoc/>
        public async Task<IBlobLockResult> LockAsync(string Key, TimeSpan? Expiration = null, CancellationToken Token = default)
        {
            var Request = LockRequest.Make(new LockRequest
            {
                Key = Key,
                Expiration = Expiration.HasValue
                    ? Expiration.Value.TotalSeconds 
                    : FileSystemBlobStorage.LOCK_EXPIRATION.TotalSeconds
            });

            try
            {
                using (var Http = await RequestAsync(HttpMethod.Post, "lock", Request, null, Token))
                {
                    if (Http.IsSuccessStatusCode)
                        return new LockResult(m_Http, await LockResponse.MakeAsync(Http.Content), Key);

                    return new LockResult((BlobStatus)Http.StatusCode);
                }
            }

            catch (OperationCanceledException) { return new LockResult(BlobStatus.Canceled); }
            catch (HttpRequestException Ex)
            {
                if (Ex.StatusCode.HasValue)
                    return new LockResult((BlobStatus)Ex.StatusCode.Value);
            }

            return new LockResult(BlobStatus.Timedout);
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> GetAsync(string Key, CancellationToken Token = default)
        {
            var Path = string.Join('/', "blob", StorageHelpers.Normalize(Key));
            return await InternalGetAsync(() => RequestAsync(HttpMethod.Get, Path, null, null, Token));
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> GetAsync(string Key, string Etag, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Etag))
                return await GetAsync(Key, Token);

            var Path = string.Join('/', "blob", StorageHelpers.Normalize(Key));
            return await InternalGetAsync(() => RequestAsync(HttpMethod.Get, Path, null, Headers => Headers.Add("If-Match", $"\"{Etag}\""), Token));
        }

        /// <summary>
        /// Send `Get` request to server.
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<IBlobResult> InternalGetAsync(Func<Task<HttpResponseMessage>> RequestAsync)
        {
            HttpResponseMessage Http = null;

            try
            {
                if ((Http = await RequestAsync()).IsSuccessStatusCode)
                {
                    var Temp = Http;
                    Http = null;

                    return new DataResult(Temp);
                }

                return new DataResult((BlobStatus)Http.StatusCode);
            }

            catch (OperationCanceledException) { return new DataResult(BlobStatus.Canceled); }
            catch (HttpRequestException Ex)
            {
                if (Ex.StatusCode.HasValue)
                    return new DataResult((BlobStatus)Ex.StatusCode.Value);
            }
            finally
            {
                Http?.Dispose();
            }

            return new DataResult(BlobStatus.Timedout);
        }

        /// <summary>
        /// Set Request Headers.
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Content"></param>
        private static HttpContent SetHeaders(ref BlobData Data, HttpContent Content)
        {
            if (!string.IsNullOrWhiteSpace(Data.Etag))
                Content.Headers.Add("If-Match", $"\"{Data.Etag}\"");

            if (Content is ByteArrayContent)
            {
                Content.Headers.Remove("Content-Type");
                Content.Headers.Add("Content-Type", "application/octet-stream");
            }

            return Content;
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> PostAsync(string Key, BlobData Data, CancellationToken Token = default)
        {
            var Content = SetHeaders(ref Data, new ByteArrayContent(Data.Data.ToArray()));
            var Path = string.Join('/', "blob", StorageHelpers.Normalize(Key));

            HttpResponseMessage Http = null;

            try
            {
                if ((Http = await RequestAsync(HttpMethod.Post, Path, Content, null, Token)).IsSuccessStatusCode)
                {
                    var Temp = Http;
                    Http = null;

                    return new DataResult(Temp, this, Key);
                }

                return new DataResult((BlobStatus)Http.StatusCode);
            }

            catch (OperationCanceledException) { return new DataResult(BlobStatus.Canceled); }
            catch (HttpRequestException Ex)
            {
                if (Ex.StatusCode.HasValue)
                    return new DataResult((BlobStatus)Ex.StatusCode.Value);
            }
            finally
            {
                Http?.Dispose();
            }

            return new DataResult(BlobStatus.Timedout);
        }

        /// <inheritdoc/>
        public async Task<IBlobResult> PutAsync(string Key, BlobData Data, CancellationToken Token = default)
        {
            var Content = SetHeaders(ref Data, new ByteArrayContent(Data.Data.ToArray()));
            var Path = string.Join('/', "blob", StorageHelpers.Normalize(Key));

            HttpResponseMessage Http = null;

            try
            {
                if ((Http = await RequestAsync(HttpMethod.Put, Path, Content, null, Token)).IsSuccessStatusCode)
                {
                    var Temp = Http;
                    Http = null;

                    return new DataResult(Temp, this, Key);
                }

                return new DataResult((BlobStatus)Http.StatusCode);
            }

            catch (OperationCanceledException) { return new DataResult(BlobStatus.Canceled); }
            catch (HttpRequestException Ex)
            {
                if (Ex.StatusCode.HasValue)
                    return new DataResult((BlobStatus)Ex.StatusCode.Value);
            }
            finally
            {
                Http?.Dispose();
            }

            return new DataResult(BlobStatus.Timedout);
        }

        /// <inheritdoc/>
        public async Task<BlobStatus> DeleteAsync(string Key, string Etag = null, CancellationToken Token = default)
        {
            var Path = string.Join('/', "blob", StorageHelpers.Normalize(Key));

            try
            {
                using var Http = await RequestAsync(HttpMethod.Delete, Path, null, Headers =>
                {
                    if (!string.IsNullOrWhiteSpace(Etag))
                        Headers.Add("If-Match", $"\"{Etag}\"");
                }, Token);
                if (Http.IsSuccessStatusCode)
                    return BlobStatus.Ok;

                return (BlobStatus)Http.StatusCode;
            }

            catch (OperationCanceledException) { return BlobStatus.Canceled; }
            catch (HttpRequestException Ex)
            {
                if (Ex.StatusCode.HasValue)
                    return (BlobStatus)Ex.StatusCode.Value;
            }

            return BlobStatus.Timedout;
        }

        /// <inheritdoc/>
        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            m_Http.Dispose();
            return ValueTask.CompletedTask;
        }

    }
}
