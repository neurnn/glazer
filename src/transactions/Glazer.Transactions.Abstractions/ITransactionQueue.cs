using Backrole.Crypto;
using Glazer.Common.Models;
using System.Collections.Generic;

namespace Glazer.Transactions.Abstractions
{
    public interface ITransactionQueue
    {
        /// <summary>
        /// Count of the queued transactions.
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// Count of the pending transactions.
        /// </summary>
        int Pendings { get; }

        /// <summary>
        /// Test whether the queue is read only or not.
        /// </summary>
        bool IsWorkingSet { get; }

        /// <summary>
        /// Test whether the queue is completed or not.
        /// </summary>
        bool Completed { get; }

        /// <summary>
        /// Clear all pending status and completed status.
        /// </summary>
        /// <returns></returns>
        bool Clear();

        /// <summary>
        /// Test whether the transaction id is contained on the queue or not.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        bool Contains(HashValue Id);

        /// <summary>
        /// Enqueue a transaction to the queue.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        TransactionRegistration Enqueue(TransactionRequest Request);

        /// <summary>
        /// Get the status of the specific transaction.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        TransactionExecutionStatus GetStatus(HashValue Id);

        /// <summary>
        /// Get queued requests.
        /// </summary>
        /// <param name="Requests"></param>
        /// <returns></returns>
        ITransactionQueue GetPendings(IList<TransactionRegistration> Requests);

        /// <summary>
        /// Get all status.
        /// </summary>
        /// <param name="Status"></param>
        /// <returns></returns>
        ITransactionQueue GetStatus(IDictionary<HashValue, TransactionExecutionStatus> Status);

        /// <summary>
        /// Set the transaction status to queued registrations.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        ITransactionQueue SetStatus(HashValue Id, TransactionExecutionStatus Status);
    }
}
