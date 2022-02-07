using System;

namespace Glazer.Nodes.Helpers
{
    public static class DateTimeHelpers
    {
        /// <summary>
        /// Ensure the date-time value to be UTC.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static DateTime EnsureUtc(this DateTime Input)
        {
            if (Input.Kind != DateTimeKind.Utc)
                Input = Input.ToUniversalTime();

            return Input;
        }

        /// <summary>
        /// Convert the date-time to Unix seconds in <see cref="double"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static double ToUnixSeconds(this DateTime Input)
        {
            return Math.Max((DateTime.UnixEpoch - Input.EnsureUtc()).TotalSeconds, 0);
        }
    }
}
