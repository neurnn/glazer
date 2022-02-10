using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Glazer.Core.Nodes.Services.Internals
{
    /// <summary>
    /// Stores values using its index.
    /// </summary>
    internal class InternalBlockDataSlots : IDisposable
    {
        private SQLiteConnection m_Sqlite;
        private const string STMT_INIT =
            "CREATE TABLE IF NOT EXISTS kv (" +
                "k INTEGER PRIMARY KEY, " +
                "v BLOB NULL DEFAULT (x'')" +
            ")";

        private const string STMT_SET =
            "INSERT OR REPLACE INTO kv (k, v) VALUES (@kk, @vv)";

        private const string STMT_GET =
            "SELECT v FROM kv WHERE k = @kk LIMIT 1";

        /// <summary>
        /// Initialize a new <see cref="InternalBlockDataSlots"/> instance.
        /// </summary>
        /// <param name="FullPath"></param>
        public InternalBlockDataSlots(string FullPath)
        {
            if (!File.Exists(FullPath))
            {
                SQLiteConnection.CreateFile(FullPath);
                IsCreatedNow = true;
            }

            (m_Sqlite = new SQLiteConnection($"Data Source={FullPath}")).Open();
            if (IsCreatedNow)
            {
                using var Command = m_Sqlite.CreateCommand();

                Command.CommandText = STMT_INIT;
                Command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Indicates whether the storage is created now or not.
        /// </summary>
        public bool IsCreatedNow { get; } = false;

        /// <summary>
        /// Last Access Time.
        /// </summary>
        public DateTime LastAccessTime { get; private set; } = DateTime.Now;

        /// <summary>
        /// Set the blob by its index.
        /// </summary>
        /// <param name="L32"></param>
        /// <param name="Blob"></param>
        /// <returns></returns>
        public bool Set(uint L32, byte[] Blob)
        {
            using var Cmd = m_Sqlite.CreateCommand();

            Cmd.CommandText = STMT_SET;
            Cmd.Parameters.Add("@kk", DbType.UInt32);
            Cmd.Parameters.Add("@vv", DbType.Binary);

            Cmd.Parameters["@kk"].Value = L32;
            Cmd.Parameters["@vv"].Value = Blob;

            try { return Cmd.ExecuteNonQuery() > 0; }
            catch { } finally { LastAccessTime = DateTime.Now; }

            return false;
        }

        /// <summary>
        /// Get the blob by its index.
        /// </summary>
        /// <param name="L32"></param>
        /// <returns></returns>
        public byte[] Get(uint L32)
        {
            using var Cmd = m_Sqlite.CreateCommand();
            byte[] Result;

            Cmd.CommandText = STMT_GET;
            Cmd.Parameters.Add("@kk", DbType.UInt32);
            Cmd.Parameters["@kk"].Value = L32;

            try   { Result = Cmd.ExecuteScalar() as byte[]; }
            catch { Result = null; } finally { LastAccessTime = DateTime.Now; }
            return  Result;
        }

        /// <summary>
        /// Dispose the <see cref="InternalBlockDataSlots"/> and internal SQLite connection.
        /// </summary>
        public void Dispose() => m_Sqlite.Dispose();
    }
}
