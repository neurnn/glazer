using Glazer.Storages.Abstractions;
using Glazer.Storages.Server.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storages.Server.Internals
{
    internal class BlobStorageProvider : IBlobStorageProvider, IAsyncDisposable
    {
        private Dictionary<string, Func<IServiceProvider, IBlobStorage>> m_Factories;
        private Dictionary<string, Task<IBlobStorage>> m_Storages;
        private IServiceProvider m_Services;

        /// <summary>
        /// Initialize a new <see cref="BlobStorageProvider"/> instance.
        /// </summary>
        /// <param name="Factories"></param>
        public BlobStorageProvider(IServiceProvider Services, Dictionary<string, Func<IServiceProvider, IBlobStorage>> Factories)
        {
            m_Services = Services;
            m_Storages = new Dictionary<string, Task<IBlobStorage>>();
            m_Factories = Factories;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            Task<IBlobStorage>[] Tasks;

            lock(this)
            {
                m_Factories.Clear();
                Tasks = m_Storages.Values.ToArray();
                m_Storages.Clear();
            }

            foreach(var Each in Tasks)
            {
                try { 
                    var Instance = await Each;

                    if (Instance is IAsyncDisposable Async)
                        await Async.DisposeAsync();

                    else if (Instance is IDisposable Sync)
                        Sync.Dispose();
                }

                catch { }
            }
        }

        /// <inheritdoc/>
        public IBlobStorage GetStorage(string Name = null)
        {
            Task<IBlobStorage> Task;

            if (string.IsNullOrWhiteSpace(Name))
                Name = "default";

            lock (this)
            {
                if (!m_Storages.TryGetValue(Name, out Task))
                {
                    var Tcs = new TaskCompletionSource<IBlobStorage>();
                    ThreadPool.QueueUserWorkItem(_ => CreateNew(Name, Tcs));

                    m_Storages[Name] = Task = Tcs.Task;
                }
            }

            return Task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Create a new storage instance using the factory delegate.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Tcs"></param>
        private void CreateNew(string Name, TaskCompletionSource<IBlobStorage> Tcs)
        {
            m_Factories.TryGetValue(Name, out var Factory);
            
            try
            {
                if (Factory != null)
                    Tcs.TrySetResult(Factory(m_Services));

                else 
                    Tcs.TrySetException(new KeyNotFoundException($"No storage exists: {Name}"));
            }

            catch(Exception e)
            {
                Tcs.TrySetException(e);
            }
        }
    }
}
