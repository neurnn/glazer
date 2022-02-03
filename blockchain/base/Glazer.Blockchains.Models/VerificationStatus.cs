namespace Glazer.Blockchains.Models
{
    public enum VerificationStatus
    {
        /// <summary>
        /// Okay, the data isn't corrupted.
        /// </summary>
        Okay = 0,

        /// <summary>
        /// Invalid Hash Value.
        /// </summary>
        HashError,

        /// <summary>
        /// Invalid Signature
        /// </summary>
        SignatureError,

        /// <summary>
        /// Incompleted, so can't verify.
        /// </summary>
        Incompleted
    }
}
