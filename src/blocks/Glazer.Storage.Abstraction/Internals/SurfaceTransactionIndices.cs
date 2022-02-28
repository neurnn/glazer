using Backrole.Crypto;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Storage.Abstraction.Internals
{
    public class SurfaceTransactionIndices
    {
        private IKvTable m_Caches;
        private IStorage m_Storage;

        /// <summary>
        /// Initialize a new <see cref="SurfaceTransactionIndices"/> instance.
        /// </summary>
        /// <param name="Storage"></param>
        /// <param name="Table"></param>
        public SurfaceTransactionIndices(IStorage Storage, IKvTable Caches)
        {
            m_Storage = Storage;
            m_Caches = Caches;
        }

        /// <summary>
        /// Get the transaction's block id asynchronously.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<BlockId> GetTransactionAsync(HashValue TransactionId, CancellationToken Token = default)
        {
            var Cursor = m_Storage.LatestBlockId;

            while(true)
            {
                var Result = await m_Caches.GetGuidAsync(TransactionId.ToString(), Token);
                if (Result != Guid.Empty)
                {
                    return new BlockId(Result);
                }

                var Block = await m_Storage.GetAsync(Cursor, Token);
                if (Block.IsValid)
                {
                    var IsHere = false;

                    foreach(var Each in Block.Transactions)
                    {
                        if (Each.Id == TransactionId)
                            IsHere = true;

                        await m_Caches.SetGuidAsync(Each.Id.ToString(), Cursor.Guid);
                    }

                    if (IsHere)
                        return Cursor;

                    Cursor = Block.Previous.Id;
                    continue;
                }

                return BlockId.Empty;
            }
        }
    }
}
