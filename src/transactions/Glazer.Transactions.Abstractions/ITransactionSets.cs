using Backrole.Crypto;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Transactions.Abstractions
{
    public interface ITransactionSets
    {
        /// <summary>
        /// Pending Set.
        /// </summary>
        ITransactionQueue PendingSet { get; }

        /// <summary>
        /// Working Set.
        /// </summary>
        ITransactionQueue WorkingSet { get; }

        /// <summary>
        /// Get the transaction status.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        TransactionExecutionStatus PeekExecutionStatus(HashValue Id);

        /// <summary>
        /// Get the task that completed when the transaction is completed.
        /// (<see cref="TransactionStatus.Completed"/>, 
        /// <see cref="TransactionStatus.NotFound"/>,
        /// <see cref="TransactionStatus.Already"/>,
        /// <see cref="TransactionStatus.SignatureError"/>,
        /// <see cref="TransactionStatus.ActionError"/>)
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        Task<TransactionExecutionStatus> WaitExecutionStatusAsync(HashValue Id, CancellationToken Token = default);
    }
}
