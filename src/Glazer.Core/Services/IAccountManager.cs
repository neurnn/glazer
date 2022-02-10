using Backrole.Crypto;
using Glazer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Services
{
    /// <summary>
    /// Account Manager instance.
    /// </summary>
    public interface IAccountManager
    {
        /// <summary>
        /// Query accounts by login name asynchronously.
        /// </summary>
        /// <param name="Login"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Account> QueryAsync(string Login, CancellationToken Token = default);

        /// <summary>
        /// Create an account with signature delegate. asynchronously.
        /// </summary>
        /// <param name="Login"></param>
        /// <param name="Stamp"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<Account> CreateAsync(string Login, Func<byte[], SignSealValue> Stamp, CancellationToken Token = default);

        /// <summary>
        /// Verify whether the <paramref name="Input"/> message is sent by the <see cref="Account"/> or not.
        /// </summary>
        /// <param name="Login"></param>
        /// <param name="Seal"></param>
        /// <param name="Input"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<HttpStatusCode> VerifyAsync(string Login, SignSealValue Seal, byte[] Input, CancellationToken Token = default);

        /// <summary>
        /// Add the public key to the account asynchronously.
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="Stamp"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<HttpStatusCode> AddKeyAsync(Account Account, Func<byte[], SignSealValue> Stamp, CancellationToken Token = default);

        /// <summary>
        /// Remove the public key from the account asynchronously.
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="Stamp"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<HttpStatusCode> RemoveKeyAsync(Account Account, Func<byte[], SignValue> Stamp, CancellationToken Token = default);

        /// <summary>
        /// Get all public keys of the specified account.
        /// </summary>
        /// <param name="Account"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<SignPublicKey[]> GetKeysAsync(Account Account, CancellationToken Token = default);
    }
}
