using Backrole.Core.Abstractions;
using Backrole.Http.Abstractions;
using Glazer.Storages.Abstractions;
using Glazer.Storages.Internals.Http;
using Glazer.Storages.Server.Abstractions;
using Glazer.Storages.Server.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storages.Server
{
    public partial class BlobStorageServerBuilder : IBlobStorageServerBuilder
    {
        private IHttpContainerBuilder m_Http;
        private List<Action<IBlobStorageServiceCollection>> m_ConfigureServices = new();
        private List<Action<IConfiguration, IServiceProvider>> m_Configures = new();
        private List<Func<IBlobContext, Func<Task>, Task>> m_Middlewares = new();

        /// <summary>
        /// Initialize a new <see cref="BlobStorageServerBuilder"/> instance.
        /// </summary>
        /// <param name="Http"></param>
        public BlobStorageServerBuilder(IHttpContainerBuilder Http) => (m_Http = Http)
            .ConfigureServices(Services =>
            {
                var BlobServices = new BlobStorageServiceCollection(Services);

                BlobServices
                    .AddScoped<IBlobContext, BlobContext>()
                    .AddSingleton<BlobLockManager>();

                foreach (var Each in m_ConfigureServices)
                    Each?.Invoke(BlobServices);
            })

            .Configure(App =>
            {
                foreach(var Each in m_Middlewares)
                    ConfigureMiddleware(App, Each);

                ConfigureFailbackToDefault(App, m_Middlewares.Count > 0);

                foreach (var Each in m_Configures)
                    Each?.Invoke(App.Configurations, App.HttpServices);
            });

        /// <summary>
        /// Adds the blob storage middleware to the <see cref="IHttpApplicationBuilder"/>.
        /// </summary>
        /// <param name="App"></param>
        /// <param name="Middleware"></param>
        private static void ConfigureMiddleware(IHttpApplicationBuilder App, Func<IBlobContext, Func<Task>, Task> Middleware)
        {
            App.Use(async (Http, Next) =>
            {
                if (Http.Services.GetService<IBlobContext>() is BlobContext Blob && Blob.IsBlobRequest)
                {
                    if (Blob.Method == BlobMethod.None)
                    {
                        Http.Response.Status = 400;
                        Http.Response.StatusPhrase = "Bad Request";
                        return;
                    }

                    if (Blob.Method == BlobMethod.Test)
                    {
                        Http.Response.Status = 200;
                        Http.Response.StatusPhrase = "OK";
                        return;
                    }

                    if (Blob.Method == BlobMethod.Unlock)
                    {
                        await Blob.SendAsync(Http);
                        return;
                    }

                    await Middleware(Blob, Next);

                    if (Blob.Result != null && Blob.LockResult != null)
                        await Blob.SendAsync(Http);

                    return;
                }

                await Next();
            });
        }

        /// <summary>
        /// Adds the fallback handler.
        /// </summary>
        /// <param name="App"></param>
        /// <param name="HasMiddlewares"></param>
        private static void ConfigureFailbackToDefault(IHttpApplicationBuilder App, bool HasMiddlewares)
        {
            App.Use(async (Http, Next) =>
            {
                if (Http.Services.GetService<IBlobContext>() is BlobContext Blob && Blob.IsBlobRequest)
                {
                    if (!HasMiddlewares)
                    {
                        if (Blob.Method == BlobMethod.None)
                        {
                            Http.Response.Status = 400;
                            Http.Response.StatusPhrase = "Bad Request";
                            return;
                        }
                        
                        if (Blob.Method == BlobMethod.Test)
                        {
                            Http.Response.Status = 200;
                            Http.Response.StatusPhrase = "OK";
                            return;
                        }

                        if (Blob.Method == BlobMethod.Unlock)
                        {
                            await Blob.SendAsync(Http);
                            return;
                        }
                    }

                    if (Blob.Result is null && Blob.LockResult is null)
                    {
                        var Provider = Http.Services.GetService<IBlobStorageProvider>();
                        var Storage = Provider != null ? Provider.GetStorage("default") : null;
                        if (Storage != null)
                        {
                            switch (Blob.Method)
                            {
                                case BlobMethod.Lock:
                                    if (Blob.Options is LockRequest LockRequest)
                                        Blob.LockResult = await Storage.LockAsync(Blob.Key, TimeSpan.FromSeconds(LockRequest.Expiration), Http.Aborted);

                                    break;

                                case BlobMethod.Create:
                                    Blob.Result = await Storage.PostAsync(Blob.Key, new BlobData
                                    {
                                        Etag = Blob.ETag,
                                        Data = await Blob.GetRequestContentAsync()
                                    }, Http.Aborted);
                                    break;

                                case BlobMethod.Read:
                                    Blob.Result = await Storage.GetAsync(Blob.Key, Blob.ETag, Http.Aborted);
                                    break;

                                case BlobMethod.Write:
                                    Blob.Result = await Storage.PutAsync(Blob.Key, new BlobData
                                    {
                                        Etag = Blob.ETag,
                                        Data = await Blob.GetRequestContentAsync()
                                    }, Http.Aborted);
                                    break;

                                case BlobMethod.Remove:
                                    Blob.Result = new StatusResult
                                    {
                                        ETag = Blob.ETag,
                                        Status = await Storage.DeleteAsync(Blob.Key, Blob.ETag, Http.Aborted)
                                    };
                                    break;
                            }
                        }
                    }

                    if (Blob.Result != null || Blob.LockResult != null)
                    {
                        if (!HasMiddlewares)
                            await Blob.SendAsync(Http);

                        return;
                    }

                    Http.Response.Status = 400;
                    Http.Response.StatusPhrase = "Bad Request";
                    return;
                }

                await Next();
            });
        }

        /// <inheritdoc/>
        public IServiceProperties Properties => m_Http.Properties;

        /// <inheritdoc/>
        public IBlobStorageServerBuilder ConfigureServices(Action<IBlobStorageServiceCollection> Delegate)
        {
            m_ConfigureServices.Add(Delegate);
            return this;
        }

        /// <inheritdoc/>
        public IBlobStorageServerBuilder Configure(Action<IConfiguration, IServiceProvider> Delegate)
        {
            m_Configures.Add(Delegate);
            return this;
        }

        /// <inheritdoc/>
        public IBlobStorageServerBuilder Use(Func<IBlobContext, Func<Task>, Task> Middleware)
        {
            m_Middlewares.Add(Middleware);
            return this;
        }
    }
    
}
