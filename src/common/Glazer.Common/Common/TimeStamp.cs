using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Common.Common
{
    public struct TimeStamp : IEquatable<TimeStamp>, IComparable<TimeStamp>
    {
        /// <summary>
        /// Epoch. that the timestamp based on.
        /// </summary>
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Initialize a new <see cref="TimeStamp"/> value;
        /// </summary>
        /// <param name="Seconds"></param>
        public TimeStamp(long Seconds) => Value = Seconds;

        /// <summary>
        /// Origin of the Time Stamp.
        /// </summary>
        public static readonly TimeStamp Zero = new TimeStamp(0);

        /// <summary>
        /// Now.
        /// </summary>
        public static TimeStamp Now => DateTime.UtcNow;

        /// <summary>
        /// Today.
        /// </summary>
        public static TimeStamp Today => DateTime.Today;

        /// <summary>
        /// Convert to <see cref="DateTime"/>.
        /// </summary>
        /// <param name="Value"></param>
        public static implicit operator DateTime(TimeStamp Value) => Value.ToDateTime();

        /// <summary>
        /// Convert from <see cref="DateTime"/>.
        /// </summary>
        /// <param name="Value"></param>
        public static implicit operator TimeStamp(DateTime Value) => new TimeStamp((long)(Value.EnforceUtc() - UNIX_EPOCH).TotalSeconds);

        /* Arithmetic Operators. */
        public static TimeStamp operator -(TimeStamp Base, long Seconds) => new TimeStamp(Base.Value - Seconds);
        public static TimeStamp operator +(TimeStamp Base, long Seconds) => new TimeStamp(Base.Value + Seconds);

        /* Comparison Operators. */
        public static bool operator ==(TimeStamp L, TimeStamp R) => L.Equals(R);
        public static bool operator !=(TimeStamp L, TimeStamp R) => !L.Equals(R);
        public static bool operator <=(TimeStamp L, TimeStamp R) => L.CompareTo(R) <= 0;
        public static bool operator >=(TimeStamp L, TimeStamp R) => L.CompareTo(R) >= 0;
        public static bool operator <(TimeStamp L, TimeStamp R) => L.CompareTo(R) < 0;
        public static bool operator >(TimeStamp L, TimeStamp R) => L.CompareTo(R) > 0;

        /// <summary>
        /// Seconds from 1970-01-01 00:00:00.0000 UTC (Origin).
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// Convert to <see cref="DateTime"/> value.
        /// </summary>
        /// <returns></returns>
        public DateTime ToDateTime(bool LocalTime = false)
        {
            var Value = UNIX_EPOCH.AddSeconds(this.Value);
            if (LocalTime && Value.Kind != DateTimeKind.Local)
                return Value.ToLocalTime();

            return Value;
        }

        /// <inheritdoc/>
        public bool Equals(TimeStamp Other) => Value == Other.Value;

        /// <inheritdoc/>
        public int CompareTo(TimeStamp Other)
        {
            var X = Value - Other.Value;

            return X < 0 ? -1 : (X > 0 ? 1 : 0);
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is TimeStamp Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => Value.ToString().TrimEnd('0', '.');
    }
}
