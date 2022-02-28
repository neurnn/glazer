using Glazer.Kvdb.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Sqlite
{
    public class SqliteKvTable : IKvTable
    {
        private SqliteKvConnection m_Connection;
        private string m_Table;

        private const string STMT_SET = "INSERT OR REPLACE INTO \"[TABLE]\" (K, V) VALUES (@KEY, @VALUE)";
        private const string STMT_GET = "SELECT V FROM \"[TABLE]\" WHERE K = @KEY LIMIT 1";
        private const string STMT_TRC = "DELETE FROM \"[TABLE]\"";

        /// <summary>
        /// Initialize a new <see cref="SqliteKvTable"/> instance.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Table"></param>
        internal SqliteKvTable(SqliteKvConnection Connection, string Table)
        {
            (m_Connection = Connection).GrabRef();
            m_Table = Table;
        }

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return null;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                using var Command = m_Connection.Sqlite.CreateCommand();

                Command.CommandText = STMT_GET.Replace("[TABLE]", m_Table);
                Command.Parameters.Add("KEY", DbType.String).Value = Key;

                try { return await Command.ExecuteScalarAsync(Token) as byte[]; }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                m_Connection.Semaphore.Release();
                m_Connection.DropRef();
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return false;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                using var Command = m_Connection.Sqlite.CreateCommand();

                Command.CommandText = STMT_SET.Replace("[TABLE]", m_Table);
                Command.Parameters.Add("KEY", DbType.String).Value = Key;
                Command.Parameters.Add("VALUE", DbType.Binary).Value = Value;

                try { return await Command.ExecuteNonQueryAsync(Token) > 0; }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                m_Connection.Semaphore.Release();
                m_Connection.DropRef();
            }

            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_Connection.DropRef();
        }

        /// <summary>
        /// Truncate the table.
        /// </summary>
        internal async Task TruncateAsync()
        {
            using var Command = m_Connection.Sqlite.CreateCommand();
            Command.CommandText = STMT_TRC.Replace("[TABLE]", m_Table);

            try { await Command.ExecuteNonQueryAsync(); }
            catch
            {
            }
        }
    }
}
