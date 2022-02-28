using System;
using System.Data.SQLite;
using System.Threading;

namespace Glazer.Kvdb.Sqlite
{
    internal class SqliteKvConnection
    {
        private int m_Refs = 1;

        /// <summary>
        /// Initialize a new <see cref="SqliteKvConnection"/> instance.
        /// </summary>
        /// <param name="Sqlite"></param>
        /// <param name="Semaphore"></param>
        public SqliteKvConnection(SQLiteConnection Sqlite)
        {
            this.Sqlite = Sqlite;
            Semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Connection Instance.
        /// </summary>
        public SQLiteConnection Sqlite { get; }

        /// <summary>
        /// Semaphore Instance.
        /// </summary>
        public SemaphoreSlim Semaphore { get; }

        /// <summary>
        /// Add reference count.
        /// </summary>
        public void GrabRef() => Interlocked.Increment(ref m_Refs);

        /// <summary>
        /// Drop the reference count.
        /// </summary>
        public void DropRef()
        {
            if (Interlocked.Decrement(ref m_Refs) == 0)
            {
                Sqlite.Dispose();
                Semaphore.Dispose();
            }
        }
    }
}
