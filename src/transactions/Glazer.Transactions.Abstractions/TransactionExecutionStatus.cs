namespace Glazer.Transactions.Abstractions
{
    public struct TransactionExecutionStatus
    {
        /// <summary>
        /// Initialize a new <see cref="TransactionExecutionStatus"/> instance.
        /// </summary>
        /// <param name="Status"></param>
        /// <param name="Reason"></param>
        public TransactionExecutionStatus(TransactionStatus Status, string Reason)
        {
            this.Status = Status;
            this.Reason = Reason;
        }

        /// <summary>
        /// Transaction Status.
        /// </summary>
        public TransactionStatus Status { get; }

        /// <summary>
        /// Reason if the status represent error.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Test whether the transaction has been completed by the engine or not.
        /// </summary>
        /// <returns></returns>
        public bool IsCompleted => Status == TransactionStatus.Completed;

        /// <summary>
        /// Test whether the transaction has been canceled by the engine.
        /// </summary>
        /// <returns></returns>
        public bool IsCanceled =>
            Status == TransactionStatus.NotFound ||
            Status == TransactionStatus.Already ||
            Status == TransactionStatus.SignatureError ||
            Status == TransactionStatus.ActionError ||
            Status == TransactionStatus.QueueError;

        public bool IsCompletedAnyway => IsCompleted || IsCanceled;
    }
}
