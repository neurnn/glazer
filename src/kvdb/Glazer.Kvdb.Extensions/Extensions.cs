using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Extensions.Internals;
using System;

namespace Glazer.Kvdb.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Disable the calling to <see cref="IDisposable.Dispose"/> method of the table.
        /// </summary>
        /// <param name="Table"></param>
        /// <returns></returns>
        public static IKvTable DisableDispose(this IKvTable Table) => new DisableDispose(Table);

        /// <summary>
        /// Separate the <see cref="IKvTable"/>'s Read and Write.
        /// </summary>
        /// <param name="Read"></param>
        /// <param name="Write"></param>
        /// <returns></returns>
        public static IKvTable Duplex(this IKvTable Read, IKvTable Write) => new Duplexer(Read, Write);

        /// <summary>
        /// Overlay the <see cref="IKvTable"/>'s Read.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Overlay"></param>
        /// <returns></returns>
        public static IKvTable Overlay(this IKvTable Table, IKvTable Overlay) => new Overlay(Table, Overlay);

        /// <summary>
        /// Append the prefix to all keys on access time.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Prefix"></param>
        /// <returns></returns>
        public static IKvTable Prefix(this IKvTable Table, string Prefix) => new Prefix(Table, Prefix);

        /// <summary>
        /// Append the postfix to all keys on access time.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Postfix"></param>
        /// <returns></returns>
        public static IKvTable Postfix(this IKvTable Table, string Postfix) => new Postfix(Table, Postfix);
    }
}
