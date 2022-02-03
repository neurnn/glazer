using Backrole.Core.Abstractions;
using Glazer.Storages.Abstractions;
using Glazer.Storages.Server.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Storages.Server.Internals
{
    internal class BlobStorageServiceCollection : IBlobStorageServiceCollection
    {
        private Dictionary<string, Func<IServiceProvider, IBlobStorage>> m_Factories = new();
        private IServiceCollection m_Services;

        /// <summary>
        /// Initialize a new <see cref="BlobStorageServiceCollection"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        public BlobStorageServiceCollection(IServiceCollection Services)
            => m_Services = Services;

        /// <inheritdoc/>
        public IServiceProperties Properties => m_Services.Properties;

        /// <inheritdoc/>
        public IServiceExtensionCollection Extensions => m_Services.Extensions;

        /// <inheritdoc/>
        public Action<IServiceProvider> ConfigureDelegate => m_Services.ConfigureDelegate;

        /// <inheritdoc/>
        public int Count => m_Services.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => m_Services.IsReadOnly;

        /// <inheritdoc/>
        public IBlobStorageServiceCollection AddStorage(string Name, Func<IServiceProvider, IBlobStorage> Factory)
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = "default";

            m_Factories[Name] = Factory;
            this.AddSingleton<IBlobStorageProvider>(Services => new BlobStorageProvider(Services, m_Factories));
            return this;
        }

        /// <inheritdoc/>
        public void Clear() => m_Services.Clear();

        /// <inheritdoc/>
        public void Add(IServiceRegistration item) => m_Services.Add(item);

        /// <inheritdoc/>
        public IServiceCollection Configure(Action<IServiceProvider> Delegate) => m_Services.Configure(Delegate);

        /// <inheritdoc/>
        public bool Contains(IServiceRegistration item) => m_Services.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(IServiceRegistration[] array, int arrayIndex) => m_Services.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public IServiceRegistration Find(Type ServiceType) => m_Services.Find(ServiceType);

        /// <inheritdoc/>
        public IServiceRegistration FindLast(Type ServiceType) => m_Services.FindLast(ServiceType);

        /// <inheritdoc/>
        public IEnumerator<IServiceRegistration> GetEnumerator() => m_Services.GetEnumerator();

        /// <inheritdoc/>
        public bool Remove(IServiceRegistration item) => m_Services.Remove(item);

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_Services).GetEnumerator();
    }
}
