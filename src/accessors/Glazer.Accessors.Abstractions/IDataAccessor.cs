using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Accessors.Abstractions
{
    /// <summary>
    /// Access to data using the <see cref="IKvTable"/>.
    /// Note that the <typeparamref name="TData"/> should be serialized using BSON or JSON.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public interface IDataAccessor<TData> where TData : struct
    {
        /// <summary>
        /// Indicates whether the <see cref="IDataAccessor{TData}"/> is read-only or not.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Script Id.
        /// </summary>
        ScriptId ScriptId { get; }

        /// <summary>
        /// Surface-Set of the blockchain.
        /// </summary>
        IKvTable SurfaceSet { get; }

        /// <summary>
        /// Capture-Set to capture changes.
        /// </summary>
        IKvTable CaptureSet { get; }

        /// <summary>
        /// Get a data by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        Task<TData?> GetDataAsync(string Key, CancellationToken Token = default);

        /// <summary>
        /// Set a data by its key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        Task<bool> SetDataAsync(string Key, TData? Data, CancellationToken Token = default);
    }
}
