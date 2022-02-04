namespace Glazer.Blockchains.Models
{
    public enum BlockStatus
    {
        /// <summary>
        /// Just created, did nothing.
        /// </summary>
        Created = 0,

        /// <summary>
        /// Pending the incoming transactions.
        /// </summary>
        Waiting,

        /// <summary>
        /// Hardening as a byte blob.
        /// </summary>
        Hardening,

        /// <summary>
        /// Hardened as a byte blob.
        /// </summary>
        Hardened
    }
}
