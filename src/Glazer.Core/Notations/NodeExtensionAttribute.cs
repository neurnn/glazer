using Backrole.Core.Abstractions;
using System;

namespace Glazer.Core.Notations
{
    /// <summary>
    /// Marks the class type as a extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeExtensionAttribute : Attribute
    {
        /// <summary>
        /// Service Type.
        /// </summary>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Service Lifetime. (Default: Singleton)
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;

        /// <summary>
        /// Test whether the service is background service or not. (Default: false)
        /// </summary>
        public bool IsBackgroundService { get; set; } = false;
    }
}
