namespace Glazer.Nodes.Abstractions
{
    public enum NodeMode
    {
        Unknown,

        /// <summary>
        /// Single Mode that is used to create a genesis block.
        /// </summary>
        Genesis,

        /// <summary>
        /// Multi Mode that is verifying requested transactions and claim to BPs.
        /// </summary>
        Multi,

        /// <summary>
        /// Multi Mode as Block Producer that generates blocks.
        /// </summary>
        Multi_BP
    }
}
