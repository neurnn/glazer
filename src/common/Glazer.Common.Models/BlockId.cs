using Backrole.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Common.Models
{
    public struct BlockId : IEquatable<BlockId>
    {
        /// <summary>
        /// Initialize a new <see cref="BlockId"/> instance.
        /// </summary>
        /// <param name="Guid"></param>
        public BlockId(Guid Guid) => this.Guid = Guid;

        /// <summary>
        /// Empty Block Id.
        /// </summary>
        public static readonly BlockId Empty = new BlockId(Guid.Empty);

        /* Comparison operators. */
        public static bool operator ==(BlockId L, BlockId R) => L.Equals(R);
        public static bool operator !=(BlockId L, BlockId R) => !L.Equals(R);

        /// <summary>
        /// Make a new block id.
        /// </summary>
        /// <returns></returns>
        public static BlockId NewBlockId()
        {
            var Id = Empty;

            while (!Id.IsValid)
                Id = new BlockId(new Guid(Rng.Make(16)));

            return Id;
        }

        /// <summary>
        /// Script Guid.
        /// </summary>
        public Guid Guid { get; }

        /// <summary>
        /// Determines the script id is valid or not.
        /// </summary>
        public bool IsValid => Guid != Guid.Empty;

        /// <inheritdoc/>
        public bool Equals(BlockId Other)
        {
            return Guid == Other.Guid;
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is BlockId Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => Guid.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => Guid.ToString();
    }
}
