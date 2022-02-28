using Glazer.Kvdb.Abstractions;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading;

namespace Glazer.Kvdb.MySQL
{
    internal class MySqlKvConnection
    {
        private int m_Refs = 1;

        private string m_ConnString;

        /// <summary>
        /// Initialize a new <see cref="MySqlKvConnection"/> instance.
        /// </summary>
        /// <param name="MySql"></param>
        public MySqlKvConnection(MySqlConnection MySql, string ConnString)
        {
            this.MySql = MySql;
            m_ConnString = ConnString;
            Semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Connection Instance.
        /// </summary>
        public MySqlConnection MySql { get; private set; }

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
                try { MySql.Dispose(); }
                catch { }

                Semaphore.Dispose();
            }
        }

        /// <summary>
        /// Throw <see cref="KvdbUnavailableException"/> exception if the connection is not opened.
        /// </summary>
        public void ThrowIfNotOpened()
        {
            if (MySql.State != ConnectionState.Open)
            {
                try { MySql.Dispose(); } catch { }
                try { (MySql = new MySqlConnection(m_ConnString)).Open(); }
                catch (Exception e)
                {
                    throw new KvdbUnavailableException("the connection is dead.", e);
                }
            }
        }

    }
}
