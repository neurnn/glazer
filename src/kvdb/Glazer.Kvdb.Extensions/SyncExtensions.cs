using Glazer.Kvdb.Abstractions;
using System.Collections.Generic;

namespace Glazer.Kvdb.Extensions
{
    public static class SyncExtensions
    {
        /// <summary>
        /// List all table names.
        /// </summary>
        /// <param name="Scheme"></param>
        /// <returns></returns>
        public static IEnumerable<string> List(this IKvScheme Scheme)
        {
            var Tables = Scheme.ListAsync();
            var Enum = Tables.GetAsyncEnumerator();

            try
            {
                while (Enum.MoveNextAsync().GetAwaiter().GetResult())
                {
                    yield return Enum.Current;
                }
            }

            finally
            {
                Enum.DisposeAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Create a new <see cref="IKvTable"/> using its name.
        /// If the table is already exists, this returns null.
        /// </summary>
        /// <param name="Scheme"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static IKvTable Create(this IKvScheme Scheme, string Name)
        {
            return Scheme.CreateAsync(Name).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Open the <see cref="IKvTable"/> using its name.
        /// </summary>
        /// <param name="Scheme"></param>
        /// <param name="Name"></param>
        /// <param name="ReadOnly"></param>
        /// <returns></returns>
        public static IKvTable Open(this IKvScheme Scheme, string Name, bool ReadOnly = false)
        {
            return Scheme.OpenAsync(Name, ReadOnly).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Drop the table.
        /// </summary>
        /// <param name="Scheme"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static bool Drop(this IKvScheme Scheme, string Name)
        {
            return Scheme.DropAsync(Name).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Drop the scheme.
        /// </summary>
        /// <param name="Scheme"></param>
        /// <returns></returns>
        public static bool Drop(this IKvScheme Scheme)
        {
            return Scheme.DropAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get the value by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static byte[] Get(this IKvTable Table, string Key)
        {
            return Table.GetAsync(Key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Set the value by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool Set(this IKvTable Table, string Key, byte[] Value)
        {
            return Table.SetAsync(Key, Value).GetAwaiter().GetResult();
        }
    }
}
