using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Abstractions
{
    /// <summary>
    /// A scheme that contains `<see cref="IKvTable"/>` instances.
    /// </summary>
    public interface IKvScheme : IDisposable
    {
        /// <summary>
        /// List all table names.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        IAsyncEnumerable<string> ListAsync(CancellationToken Token = default);

        /// <summary>
        /// Create a new <see cref="IKvTable"/> using its name.
        /// If the table is already exists, this returns null.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<IKvTable> CreateAsync(string Name, CancellationToken Token = default);

        /// <summary>
        /// Open the <see cref="IKvTable"/> using its name.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Token"></param>
        /// <param name="ReadOnly"></param>
        /// <returns></returns>
        Task<IKvTable> OpenAsync(string Name, bool ReadOnly = false, bool Truncate = false, CancellationToken Token = default);

        /// <summary>
        /// Drop the table.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> DropAsync(string Name, CancellationToken Token = default);

        /// <summary>
        /// Drop the scheme.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> DropAsync(CancellationToken Token = default);
    }
}
