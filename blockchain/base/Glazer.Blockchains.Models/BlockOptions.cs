using System;

namespace Glazer.Blockchains.Models
{
    public sealed class BlockOptions
    {
        /// <summary>
        /// Origin Time of the blockchain.
        /// </summary>
        public DateTime Epoch { get; set; } = new DateTime(2022, 2, 3, 10, 37, 0, DateTimeKind.Utc);

        /// <summary>
        /// Genesis Block Id.
        /// </summary>
        public Guid InitialBlockId { get; set; } = new Guid("fd4f7163-ce0b-414e-b49d-59a0e1f2c9a0");
    }
}
