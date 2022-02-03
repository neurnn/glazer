using System;

namespace Glazer.Blockchains.Models.Internals
{
    internal static class DateTimeExtensions
    {
        /// <summary>
        /// To Seconds that based on <paramref name="Epoch"/>
        /// </summary>
        /// <param name="Time"></param>
        /// <param name="Epoch"></param>
        /// <returns></returns>
        public static double ToSeconds(this DateTime Time, DateTime Epoch)
        {
            if (Epoch.Kind != DateTimeKind.Utc)
                Epoch = Epoch.ToUniversalTime();

            if (Time.Kind != DateTimeKind.Utc)
                Time = Time.ToUniversalTime();

            return Math.Max((Time - Epoch).TotalSeconds, 0);
        }

        /// <summary>
        /// To DateTime.
        /// </summary>
        /// <param name="Seconds"></param>
        /// <param name="Epoch"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this double Seconds, DateTime Epoch)
        {
            if (Epoch.Kind != DateTimeKind.Utc)
                Epoch = Epoch.ToUniversalTime();

            return Epoch.AddSeconds(Math.Max(Seconds, 0));
        }
    }
}
