using System;

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
    }
}
