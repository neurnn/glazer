using Glazer.Core.Cryptography;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    /// <summary>
    /// Tracks the record's history.
    /// </summary>
    public interface IRecordTracker
    {
        /// <summary>
        /// Key to track.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Read the next tracking result.
        /// </summary>
        /// <returns></returns>
        Task<bool> NextAsync(CancellationToken Token = default);

        /// <summary>
        /// Current Record.
        /// </summary>
        Record Current { get; }
    }
}
