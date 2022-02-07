
namespace Glazer.Nodes.Models.Transactions
{
    public enum TransactionStatus : byte
    {
        /// <summary>
        /// Created and not propagated.
        /// </summary>
        Created = 0,

        /// <summary>
        /// Emulating the transaction in local. (sender only)
        /// </summary>
        Emulating = 30,

        /// <summary>
        /// Witness Wanted.
        /// </summary>
        WitnessWanted= 60,

        /// <summary>
        /// Emulating the transaction on the sandbox. (witness only)
        /// </summary>
        EmulatingInSandbox = 90,

        /// <summary>
        /// Denied due to precondition unmet.
        /// </summary>
        Denied = 120,

        /// <summary>
        /// Transaction executed successfully.
        /// </summary>
        Executed = 150,

        /// <summary>
        /// Failed by the code causes VM error.
        /// </summary>
        Failure = 180
    }
}
