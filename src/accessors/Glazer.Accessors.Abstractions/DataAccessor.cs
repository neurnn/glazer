using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Accessors.Abstractions
{
    /// <summary>
    /// Default Accessor Implementation.
    /// Note that the <typeparamref name="TData"/> should be serialized using BSON or JSON.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class DataAccessor<TData> : IDataAccessor<TData>, IDisposable where TData : struct
    {
        private IKvTable m_Table;

        /// <summary>
        /// Initialize a new <see cref="DataAccessor{TData}"/> instance.
        /// </summary>
        /// <param name="SurfaceSet"></param>
        /// <param name="CaptureSet"></param>
        public DataAccessor(ScriptId ScriptId, IKvTable SurfaceSet, IKvTable CaptureSet = null)
        {
            if (!(this.ScriptId = ScriptId).IsValid)
                throw new InvalidOperationException("No ScriptId is valid.");

            this.SurfaceSet = (SurfaceSet ?? throw new ArgumentNullException(nameof(SurfaceSet))).DisableDispose();
            if ((this.CaptureSet = CaptureSet) is not null)
                m_Table = this.CaptureSet.DisableDispose().Overlay(this.SurfaceSet);

            else
                m_Table = this.SurfaceSet;
        }

        /// <inheritdoc/>
        public ScriptId ScriptId { get; }

        /// <inheritdoc/>
        public bool IsReadOnly => CaptureSet is null;

        /// <inheritdoc/>
        public IKvTable SurfaceSet { get; }

        /// <inheritdoc/>
        public IKvTable CaptureSet { get; }

        /// <inheritdoc/>
        public virtual async Task<TData?> GetDataAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return null;

            var Json = await m_Table.GetBsonObjectAsync($"{ScriptId}:{Key}", Token);

            if (Json != null)
            {
                try { return Json.ToObject<TData>(); }
                catch { }
            }

            return null;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> SetDataAsync(string Key, TData? Data, CancellationToken Token = default)
        {
            if (IsReadOnly)
                return false;

            if (Data.HasValue)
                return await m_Table.SetBsonObjectAsync($"{ScriptId}:{Key}", JObject.FromObject(Data.Value), Token);

            return await m_Table.SetBsonObjectAsync($"{ScriptId}:{Key}", null, Token);
        }

        /// <inheritdoc/>
        public void Dispose() => m_Table.Dispose();
    }
}
