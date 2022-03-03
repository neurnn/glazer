using System;
using System.Globalization;

namespace Glazer
{
    public static class DateTimeHelpers
    {
        /// <summary>
        /// Enforce the <see cref="DateTime"/> value to be <see cref="DateTimeKind.Utc"/>.
        /// </summary>
        /// <param name="DateTime"></param>
        /// <returns></returns>
        public static DateTime EnforceUtc(this DateTime DateTime)
        {
            if (DateTime.Kind != DateTimeKind.Utc)
                return DateTime.ToUniversalTime();

            return DateTime;
        }

        /// <summary>
        /// Parse RFC-1123 Date.
        /// </summary>
        /// <param name="Rfc1123"></param>
        /// <returns></returns>
        public static DateTime ParseRfc1123(string Rfc1123)
        {
            return DateTime.ParseExact(Rfc1123, "r",
                CultureInfo.InvariantCulture).EnforceUtc();
        }

        /// <summary>
        /// Try to parse RFC-1123 Date.
        /// </summary>
        /// <param name="Rfc1123"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        public static bool TryParseRfc1123(string Rfc1123, out DateTime Result)
        {
            try { Result = ParseRfc1123(Rfc1123); }
            catch
            {
                Result = default;
                return false;
            }

            return true;
        }
    }
}
