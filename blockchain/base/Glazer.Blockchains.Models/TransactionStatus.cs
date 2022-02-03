namespace Glazer.Blockchains.Models
{
    public enum TransactionStatus
    {
        /// <summary>
        /// Just created, did nothing.
        /// </summary>
        Created = 0,

        /// <summary>
        /// Transaction has been propagated and taking votes from remote nodes.
        /// </summary>
        Voting,

        /// <summary>
        /// Remote nodes gave the feed back about the transaction.
        /// </summary>
        Voted,

        /// <summary>
        /// Hardening the transaction to block.
        /// </summary>
        Hardening,

        /// <summary>
        /// Hardened as a payload of the block.
        /// </summary>
        Hardened,

        /// <summary>
        /// Denied from the remote peers.
        /// </summary>
        Denied
    }
}
