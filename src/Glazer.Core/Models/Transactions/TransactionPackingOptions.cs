namespace Glazer.Core.Models.Transactions
{
    public struct TransactionPackingOptions
    {
        /// <summary>
        /// Packs the transaction to generate <see cref="TransactionHeader.TrxId"/>.
        /// </summary>
        public static readonly TransactionPackingOptions TrxId = new() { WithId = false, WithSenderSeal = false, WithWitness = false };

        /// <summary>
        /// Packs the transaction to generate <see cref="TransactionWitness.AccountSeals"/>.
        /// </summary>
        public static readonly TransactionPackingOptions Witness = new() { WithId = true, WithSenderSeal = true, WithWitness = false };

        /// <summary>
        /// Packs the transaction to hardening into the block.
        /// </summary>
        public static readonly TransactionPackingOptions Full = new() { WithId = true, WithSenderSeal = true, WithWitness = true };

        /// <summary>
        /// Packs the transaction to generate <see cref="Backrole.Crypto.SignValue"/> to verify the transaction itself.
        /// </summary>
        public static readonly TransactionPackingOptions Verification = new() { WithId = true, WithSenderSeal = true, WithWitness = true };

        /// <summary>
        /// Includes the <see cref="TransactionHeader.TrxId"/>.
        /// </summary>
        public bool WithId;

        /// <summary>
        /// Includes the <see cref="TransactionHeader.SenderSeal"/>;
        /// </summary>
        public bool WithSenderSeal;

        /// <summary>
        /// Includes the <see cref="Transaction.Witness"/>.
        /// </summary>
        public bool WithWitness;
    }
}
