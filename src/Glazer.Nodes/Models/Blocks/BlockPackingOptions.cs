namespace Glazer.Nodes.Models.Blocks
{
    public struct BlockPackingOptions
    {
        /// <summary>
        /// <see cref="BlockHeader.Hash"/> Generation purpose.
        /// </summary>
        public static readonly BlockPackingOptions Hash = new BlockPackingOptions
        {
            WithHash = false,
            WithWitness = false,
            WithProducer = false
        };

        /// <summary>
        /// <see cref="BlockWitness"/> generation purpose.
        /// </summary>
        public static readonly BlockPackingOptions Witness = new BlockPackingOptions()
        {
            WithHash = true,
            WithWitness = false,
            WithProducer = false
        };

        /// <summary>
        /// <see cref="Block.ProducerSign"/> generation purpose.
        /// </summary>
        public static readonly BlockPackingOptions Producer = new BlockPackingOptions()
        {
            WithHash = true,
            WithWitness = true,
            WithProducer = false
        };

        /// <summary>
        /// <see cref="BlockHeader.PrevBlockHash"/> generation purpose at next block creating.
        /// </summary>
        public static readonly BlockPackingOptions Reference = new BlockPackingOptions()
        {
            WithHash = true,
            WithWitness = true,
            WithProducer = true
        };

        /// <summary>
        /// With block hash.
        /// </summary>
        public bool WithHash { get; set; }

        /// <summary>
        /// With witness
        /// </summary>
        public bool WithWitness { get; set; }

        /// <summary>
        /// With producer information.
        /// </summary>
        public bool WithProducer { get; set; }
    }
}
