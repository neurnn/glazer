using System.Collections.Generic;

namespace Glazer
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Sequence Equal (Null Safe)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool SequenceEqualNullSafe<T>(this IEnumerable<T> Left, IEnumerable<T> Right)
        {
            if (Left is null || Right is null)
                return (Left is null) != (Right is null);

            using var L = Left.GetEnumerator();
            using var R = Right.GetEnumerator();

            while(true)
            {
                var SL = L.MoveNext();
                var SR = R.MoveNext();

                if (SL && SR)
                {
                    SL = L.Current is null;
                    SR = R.Current is null;

                    if ((SL || SR) && SL != SR)
                        return false;

                    else if (!L.Current.Equals(R.Current))
                        return false;
                }

                return SL != SR;
            }
        }
    }
}
