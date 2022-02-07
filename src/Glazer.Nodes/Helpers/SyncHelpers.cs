using System.Collections.Generic;

namespace Glazer.Nodes.Helpers
{
    public static class SyncHelpers
    {
        /// <summary>
        /// Add an element to the collection with lock keyword.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Collection"></param>
        /// <param name="Value"></param>
        public static void AddLocked<T>(this ICollection<T> Collection, T Value)
        {
            lock (Collection)
            {
                Collection.Add(Value);
            }
        }

        /// <summary>
        /// Remove an element to the collection with lock keyword.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Collection"></param>
        /// <param name="Value"></param>
        public static bool RemoveLocked<T>(this ICollection<T> Collection, T Value)
        {
            lock (Collection)
            {
                return Collection.Remove(Value);
            }
        }

        /// <summary>
        /// To Queue with lock keyword.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Enumerable"></param>
        /// <returns></returns>
        public static Queue<T> ToQueueLocked<T>(this IEnumerable<T> Enumerable)
        {
            lock(Enumerable)
            {
                return new Queue<T>(Enumerable);
            }
        }

        /// <summary>
        /// Gets the value of the field with lock keyword.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Any"></param>
        /// <param name="Field"></param>
        /// <returns></returns>
        public static T LockedGet<T>(this object Any, ref T Field)
        {
            lock (Any)
                return Field;
        }
    }
}
