using Backrole.Crypto;

namespace Glazer.Transactions.Abstractions
{
    public enum TransactionStatus
    {
        /// <summary>
        /// the transaction id is not found. (Runtime Only)
        /// </summary>
        NotFound,

        /// <summary>
        /// the transaction has been queued.
        /// </summary>
        Queued,

        /// <summary>
        /// the transaction has been executed and packed at the block.
        /// </summary>
        Completed,

        /// <summary>
        /// the requested transaction has been executed (or queued) already. (Error)
        /// </summary>
        Already,

        /// <summary>
        /// the transaction's signature is missing or mismatched.
        /// </summary>
        SignatureError,

        /// <summary>
        /// the transaction's action has been failed or missing.
        /// </summary>
        ActionError,

        /// <summary>
        /// the transaction request couldn't be enqueued due to the queue instance.
        /// </summary>
        QueueError
    }
}
