using Glazer.Core.Cryptography;
using System;

namespace Glazer.Blockchains.Models
{
    public struct TransactionRef : IEquatable<TransactionRef>
    {
        /// <summary>
        /// Initialize a new <see cref="TransactionRef"/>.
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Transaction"></param>
        public TransactionRef(Block Block, Transaction Transaction)
        {
            BlockId = Block.Header.Guid;
            Hash = Transaction.Hash;
        }

        /// <summary>
        /// Initialize a new <see cref="TransactionRef"/>.
        /// </summary>
        /// <param name="BlockId"></param>
        /// <param name="Hash"></param>
        public TransactionRef(Guid BlockId, HashValue Hash)
        {
            this.BlockId = BlockId;
            this.Hash = Hash;
        }

        public static bool operator ==(TransactionRef Left, TransactionRef Right) =>  Left.Equals(Right);
        public static bool operator !=(TransactionRef Left, TransactionRef Right) => !Left.Equals(Right);

        /// <summary>
        /// Indicates whether the reference is valid or not.
        /// </summary>
        public bool IsValid => BlockId != Guid.Empty && Hash != HashValue.Empty;

        /// <summary>
        /// Block Id that the transaction hardened.
        /// </summary>
        public Guid BlockId { get; set; }

        /// <summary>
        /// Hash Value of the transaction.
        /// </summary>
        public HashValue Hash { get; set; }

        /// <summary>
        /// Try to parse the input to <see cref="TransactionRef"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out TransactionRef Output)
        {
            if (!string.IsNullOrWhiteSpace(Input))
            {
                var Tokens = Input.Split('/', 2);
                if (Tokens.Length == 2 &&
                    Guid.TryParse(Tokens[0], out var BlockId) &&
                    HashValue.TryParse(Tokens[1], out var Hash))
                {
                    Output = new TransactionRef(BlockId, Hash);
                    return true;
                }
            }

            Output = default;
            return false;
        }

        /// <summary>
        /// Parse the input to <see cref="TransactionRef"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static TransactionRef Parse(string Input)
        {
            if (TryParse(Input, out var Result))
                return Result;

            throw new ArgumentException("the input string is invalid.");
        }

        /// <summary>
        /// Make the string expression of the ref.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IsValid)
                return $"{BlockId}/{Hash}";

            return "null";
        }

        /// <inheritdoc/>
        public bool Equals(TransactionRef Other)
        {
            if (IsValid == Other.IsValid && !IsValid)
                return true;

            return BlockId == Other.BlockId && Hash == Other.Hash;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is TransactionRef Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(BlockId, Hash);
    }
}
