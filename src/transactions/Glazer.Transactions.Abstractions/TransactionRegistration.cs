using Backrole.Crypto;
using Glazer.Common.Models;

namespace Glazer.Transactions.Abstractions
{
    public struct TransactionRegistration
    {
        /// <summary>
        /// Initialize a new <see cref="TransactionRegistration"/> instance.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Request"></param>
        public TransactionRegistration(TransactionRequest Request, TransactionStatus Status)
        {
            HashValue Id = HashValue.Empty;

            if (Status != TransactionStatus.SignatureError)
                Id = Request.CalculateTransactionId();

            this.Id = Id;
            this.Status = Status;
            this.Request = Request;
        }

        /// <summary>
        /// Initialize a new <see cref="TransactionRegistration"/> instance.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Request"></param>
        public TransactionRegistration(HashValue Id, TransactionRequest Request, TransactionStatus Status)
        {
            this.Id = Id;
            this.Status = Status;
            this.Request = Request;
        }

        /// <summary>
        /// Transaction Id.
        /// </summary>
        public HashValue Id { get; }

        /// <summary>
        /// Transaction Request.
        /// </summary>
        public TransactionRequest Request { get; }

        /// <summary>
        /// Transaction Status.
        /// </summary>
        public TransactionStatus Status { get; }

        /// <summary>
        /// Test whether the transaction has been canceled by the engine.
        /// </summary>
        /// <returns></returns>
        public bool IsCanceled()
        {
            return 
                Status == TransactionStatus.Already ||
                Status == TransactionStatus.SignatureError ||
                Status == TransactionStatus.ActionError ||
                Status == TransactionStatus.QueueError;
        }
    }
}
