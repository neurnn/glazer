using Backrole.Crypto;
using Newtonsoft.Json.Linq;
using System;

namespace Glazer.Common.Models
{
    public struct BlockRef : IEquatable<BlockRef>
    {
        /// <summary>
        /// Initialize a new <see cref="BlockRef"/> instance.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Hash"></param>
        public BlockRef(BlockId Id, HashValue Hash)
        {
            this.Id = Id;
            this.Hash = Hash;
        }

        /* Comparison operators. */
        public static bool operator ==(BlockRef L, BlockRef R) => L.Equals(R);
        public static bool operator !=(BlockRef L, BlockRef R) => !L.Equals(R);

        /// <summary>
        /// Try to export <see cref="BlockRef"/> to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static bool TryExport(JObject Json, BlockRef BlockRef)
        {
            if (BlockRef.IsValid)
            {
                Json["id"] = BlockRef.Id.ToString();
                Json["hash"] = BlockRef.Hash.ToString();
                return true;
            }

            Json["id"] = null;
            Json["hash"] = null;
            return true;
        }

        /// <summary>
        /// Try to import <see cref="BlockRef"/> from <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="BlockRef"></param>
        /// <returns></returns>
        public static bool TryImport(JObject Json, out BlockRef BlockRef)
        {
            var IdStr = Json.Value<string>("id");
            var HashStr = Json.Value<string>("hash");

            if (string.IsNullOrWhiteSpace(IdStr) ||
                string.IsNullOrWhiteSpace(HashStr) ||
                !Guid.TryParse(IdStr, out var Id) ||
                !HashValue.TryParse(HashStr, out var Hash))
                return ModelHelpers.Return(true, out BlockRef);

            BlockRef = new BlockRef(new BlockId(Id), Hash);
            return true;
        }

        /// <summary>
        /// the block id.
        /// </summary>
        public BlockId Id { get; }

        /// <summary>
        /// Hash value.
        /// </summary>
        public HashValue Hash { get; }

        /// <summary>
        /// Determines whether the block is valid or not.
        /// </summary>
        public bool IsValid => Id.IsValid && Hash.IsValid;

        /// <inheritdoc/>
        public bool Equals(BlockRef Other) => Id == Other.Id && Hash == Other.Hash;

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is BlockRef Ref)
                return Equals(Ref);

            if (Input is Block Block)
                return Equals(Block);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Id, Hash);
    }
}
