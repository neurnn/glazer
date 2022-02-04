using Glazer.Core.Cryptography;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IRecordView
    {
        /// <summary>
        /// Code ID that this record repository is related with.
        /// </summary>
        Guid CodeId { get; }

        /// <summary>
        /// Key to query
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Initialize a new record for the account.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Record> NewAsync(CancellationToken Token = default);

        /// <summary>
        /// Get the record for the account.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Record> GetAsync(CancellationToken Token = default);

        /// <summary>
        /// Set the record with previous ETag against conflict state.
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> SetAsync(Record Data, CancellationToken Token = default);
    }
}
