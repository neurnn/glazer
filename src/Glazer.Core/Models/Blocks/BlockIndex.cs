using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Models.Blocks
{
    /// <summary>
    /// Represents the index of the block.
    /// </summary>
    public struct BlockIndex : IEquatable<BlockIndex>, IComparable<BlockIndex>
    {
        /// <summary>
        /// Zero Value.
        /// </summary>
        public static readonly BlockIndex Invalid = new BlockIndex(0, 0);

        /// <summary>
        /// One Value.
        /// </summary>
        public static readonly BlockIndex Genesis = new BlockIndex(0, 1);

        /// <summary>
        /// Minimum Value.
        /// </summary>
        public static readonly BlockIndex MinValue = new BlockIndex(0, 0);

        /// <summary>
        /// Maximum Value.
        /// </summary>
        public static readonly BlockIndex MaxValue = new BlockIndex(uint.MaxValue, uint.MaxValue);

        /// <summary>
        /// Initialize a new <see cref="BlockIndex"/> value.
        /// </summary>
        /// <param name="H32"></param>
        /// <param name="L32"></param>
        public BlockIndex(uint H32, uint L32)
        {
            this.H32 = H32;
            this.L32 = L32;
        }

        /// <summary>
        /// Initialize a new <see cref="BlockIndex"/> value.
        /// </summary>
        /// <param name="R64"></param>
        public BlockIndex(ulong R64)
        {
            var Temp = FromU64(R64);

            H32 = Temp.H32;
            L32 = Temp.L32;
        }

        /* ulong, long to BlockIndex. */
        public static implicit operator BlockIndex(long U64) => new BlockIndex((ulong) Math.Max(U64, 0));
        public static implicit operator BlockIndex(ulong U64) => new BlockIndex(U64);

        /* Comparison operators. */
        public static bool operator ==(BlockIndex L, BlockIndex R) => L.Equals(R);
        public static bool operator !=(BlockIndex L, BlockIndex R) => !L.Equals(R);
        public static bool operator <(BlockIndex L, BlockIndex R) => L.CompareTo(R) < 0;
        public static bool operator <=(BlockIndex L, BlockIndex R) => L.CompareTo(R) <= 0;
        public static bool operator >(BlockIndex L, BlockIndex R) => L.CompareTo(R) > 0;
        public static bool operator >=(BlockIndex L, BlockIndex R) => L.CompareTo(R) >= 0;

        /* Arithmetic operators. */
        public static BlockIndex operator ++(BlockIndex V) => V.Next();
        public static BlockIndex operator --(BlockIndex V) => V.Prev();
        public static BlockIndex operator +(BlockIndex L, BlockIndex R) => L.Add(R);
        public static BlockIndex operator -(BlockIndex L, BlockIndex R) => L.Subtract(R);

        /// <summary>
        /// Try to parse the <paramref name="Input"/> to <paramref name="Output"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out BlockIndex Output)
        {
            if (ulong.TryParse(Input, out var R64))
            {
                Output = FromU64(R64);
                return true;
            }

            Output = Invalid;
            return false;
        }

        /// <summary>
        /// High 32 bits
        /// </summary>
        public uint H32 { get; }

        /// <summary>
        /// Low 32 bits.
        /// </summary>
        public uint L32 { get; }

        /// <summary>
        /// Test whether the block index is same or not.
        /// </summary>
        /// <param name="Other"></param>
        /// <returns></returns>
        public bool Equals(BlockIndex Other) => CompareTo(Other) == 0;

        /// <inheritdoc/>
        public override bool Equals(object Object)
        {
            if (Object is BlockIndex Index)
                return CompareTo(Index) == 0;

            return false;
        }

        /// <inheritdoc/>
        public int CompareTo(BlockIndex Index)
        {
            if (H32 != Index.H32)
                return H32 < Index.H32 ? -1 : 1;

            if (L32 != Index.L32)
                return L32 < Index.L32 ? -1 : 1;

            return 0;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(H32, L32);

        /// <inheritdoc/>
        public override string ToString() => ToU64().ToString();

        /// <summary>
        /// Make U64 Value.
        /// </summary>
        /// <returns></returns>
        private readonly ulong ToU64() => (ulong)(H32 << 32) | L32;

        /// <summary>
        /// Make <see cref="BlockIndex"/> from U64 Value.
        /// </summary>
        /// <param name="R64"></param>
        /// <returns></returns>
        private static BlockIndex FromU64(ulong R64)
        {
            var H32 = (uint)(R64 >> 32);
            var L32 = (uint)(R64 & uint.MaxValue);
            return new BlockIndex(H32, L32);
        }

        /// <summary>
        /// Gets the next index.
        /// </summary>
        /// <returns></returns>
        public BlockIndex Next()
        {
            var R64 = ToU64();
            if (R64 == ulong.MaxValue)
                throw new OverflowException("Out of range.");

            var H32 = (uint)((++R64) >> 32);
            var L32 = (uint)(R64 & uint.MaxValue);
            return new BlockIndex(H32, L32);
        }

        /// <summary>
        /// Gets the previous index.
        /// </summary>
        /// <returns></returns>
        public BlockIndex Prev()
        {
            var R64 = ToU64();
            if (R64 == ulong.MinValue)
                throw new OverflowException("Out of range.");

            return FromU64(--R64);
        }

        /// <summary>
        /// Adds two index. (this + Index)
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public BlockIndex Add(BlockIndex Index)
        {
            var R64 = ToU64() + Index.ToU64();
            if (R64 > uint.MaxValue)
                throw new OverflowException("Out of range.");

            return FromU64(++R64);
        }

        /// <summary>
        /// Subtracts two index. (this - Index)
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public BlockIndex Subtract(BlockIndex Index)
        {
            var L64 = ToU64();
            var R64 = Index.ToU64();
            if (L64 < R64)
                throw new OverflowException("Out of range.");

            return FromU64(L64 - R64);
        }

        /// <summary>
        /// Make the partitioning numbers that is recommended as the glazer default..
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="S"></param>
        /// <param name="P"></param>
        /// <param name="N"></param>
        public static void MakePartitionNumbers(BlockIndex Index, out uint S, out uint P, out uint N)
        {
            S = Index.H32;
            P = Index.L32 & 0xffffff00u;
            N = Index.L32 & 0x000000ffu;
        }
    }
}
