using Backrole.Core.Abstractions;
using Backrole.Http;
using Backrole.Http.Abstractions;
using Glazer.Storages.Abstractions;
using Glazer.Storages.Internals.Http;
using Glazer.Storages.Server.Abstractions;
using System;

namespace Glazer.Storages.Server
{
    public static class BlobStorageServerExtensions
    {
        /// <summary>
        /// Adds a delegate that configures the blob storage server container to the host builder.
        /// </summary>
        /// <param name="Host"></param>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        public static IHostBuilder ConfigureBlobStorageServerContainer(this IHostBuilder Host, Action<IBlobStorageServerBuilder> Delegate)
        {
            return Host.ConfigureHttpContainer(Http => Http.ConfigureBlobStorageServer(Delegate));
        }

        /// <summary>
        /// Adds a delegate that configures the blob storage server for the Http container.
        /// </summary>
        /// <param name="Http"></param>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        public static IHttpContainerBuilder ConfigureBlobStorageServer(this IHttpContainerBuilder Http, Action<IBlobStorageServerBuilder> Delegate)
        {
            var Builder = Http.Properties
                .GetValue(typeof(BlobStorageServerExtensions), () => new BlobStorageServerBuilder(Http));

            Delegate?.Invoke(Builder);
            return Http;
        }

        /// <summary>
        /// Adds a factory delegate and a middleware that redirect all requests to the storage.
        /// </summary>
        /// <param name="Blob"></param>
        /// <param name="Name"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        public static IBlobStorageServerBuilder Use(this IBlobStorageServerBuilder Blob, string Name, Func<IServiceProvider, IBlobStorage> Factory)
        {
            return Blob
                .ConfigureServices(Services => Services.AddStorage(Name, Factory))
                .Use(async (Blob, Next) =>
                {
                    var Provider = Blob.HttpContext.Services.GetService<IBlobStorageProvider>();
                    var Storage = Provider != null ? Provider.GetStorage(Name) : null;
                    if (Storage != null)
                    {
                        switch (Blob.Method)
                        {
                            case BlobMethod.Lock:
                                if (Blob.Options is LockRequest LockRequest)
                                    Blob.LockResult = await Storage.LockAsync(Blob.Key, TimeSpan.FromSeconds(LockRequest.Expiration), Blob.HttpContext.Aborted);

                                else
                                    Blob.LockResult = new LockStatusResult { Status = BlobStatus.BadRequest };

                                break;

                            case BlobMethod.Create:
                                Blob.Result = await Storage.PostAsync(Blob.Key, new BlobData
                                {
                                    Etag = Blob.ETag,
                                    Data = await Blob.GetRequestContentAsync()
                                }, Blob.HttpContext.Aborted);
                                break;

                            case BlobMethod.Read:
                                Blob.Result = await Storage.GetAsync(Blob.Key, Blob.ETag, Blob.HttpContext.Aborted);
                                break;

                            case BlobMethod.Write:
                                Blob.Result = await Storage.PutAsync(Blob.Key, new BlobData
                                {
                                    Etag = Blob.ETag,
                                    Data = await Blob.GetRequestContentAsync()
                                }, Blob.HttpContext.Aborted);
                                break;

                            case BlobMethod.Remove:
                                Blob.Result = new StatusResult
                                {
                                    ETag = Blob.ETag,
                                    Status = await Storage.DeleteAsync(Blob.Key, Blob.ETag, Blob.HttpContext.Aborted)
                                };
                                break;
                        }

                        /* And fallback to next handler if the request couldn't be handled. */
                        if (Blob.LockResult != null && Blob.LockResult.Status == BlobStatus.NotFound)
                        {
                            try { await Blob.LockResult.DisposeAsync(); }
                            catch { }

                            Blob.LockResult = null;

                            await Next();
                            return;
                        }

                        if (Blob.Result != null && Blob.Result.Status == BlobStatus.NotFound)
                        {
                            try { await Blob.Result.DisposeAsync(); }
                            catch { }

                            Blob.Result = null;

                            await Next();
                            return;
                        }

                        return;
                    }

                    await Next();
                });
        }
    }
    
}
