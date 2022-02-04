namespace Glazer.Blockchains.Models
{
    public enum CodeRefType
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Custom Code Kind.
        /// </summary>
        Custom,

        /// <summary>
        /// Prebuilt, aka Native feature.
        /// </summary>
        Prebuilt,

        /// <summary>
        /// Code Reference that stored in the code repository.
        /// </summary>
        Reference,

        /// <summary>
        /// JavaScript. (Jint, ECMAScript 2019)
        /// </summary>
        JavaScript_Jint,

    }
}
