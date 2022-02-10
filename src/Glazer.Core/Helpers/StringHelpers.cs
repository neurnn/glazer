using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Helpers
{
    public static class StringHelpers
    {

        /// <summary>
        /// Test whether the <paramref name="Input"/> string is null/whitespace only or not.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static bool IsMeaningless(this string Input)
            => string.IsNullOrWhiteSpace(Input);

        /// <summary>
        /// Test whether two strings are equalivant in case-insensitive.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool CaseEquals(this string Left, string Right)
            => Left.Equals(Right, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Test whether the <paramref name="Input"/> string is consisted with only allowed charactors.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Allows"></param>
        /// <returns></returns>
        public static bool ConsistedOnlyWith(this string Input, string Allows) 
            => Input.Count(Allows.Contains) == Input.Length;
    }
}
