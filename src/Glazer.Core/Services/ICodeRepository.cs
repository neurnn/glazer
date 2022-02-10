using Backrole.Crypto;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Core.Services
{
    /// <summary>
    /// Code Repository instance.
    /// </summary>
    public interface ICodeRepository
    {
        /// <summary>
        /// Check the given code id exists or not.
        /// </summary>
        /// <param name="CodeId"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<HttpStatusCode> CheckAsync(Guid CodeId, CancellationToken Token = default);

        
    }
}
