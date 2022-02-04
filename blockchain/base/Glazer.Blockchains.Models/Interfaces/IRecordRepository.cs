using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    /// <summary>
    /// Saves the record of transaction or Loads saved records.
    /// </summary>
    public interface IRecordRepository
    {
        /// <summary>
        /// Code ID that this record repository is related with.
        /// </summary>
        Guid CodeId { get; }

        /// <summary>
        /// Create a view that scopes the specific account.
        /// </summary>
        /// <param name="Account"></param>
        /// <returns></returns>
        IRecordView CreateView(Account Account);

        /// <summary>
        /// Initialize a record for the account.
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Record> NewAsync(Account Account, CancellationToken Token = default);

        /// <summary>
        /// Get the record for the account.
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Record> GetAsync(Account Account, CancellationToken Token = default);

        /// <summary>
        /// Set the record with previous ETag against conflict state.
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="Object"></param>
        /// <returns></returns>
        Task<bool> SetAsync(Account Account, JObject Object, Guid? Etag = null, CancellationToken Token = default);
    }
}
