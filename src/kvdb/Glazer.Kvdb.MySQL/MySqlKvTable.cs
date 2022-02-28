using Glazer.Kvdb.Abstractions;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.MySQL
{
    public class MySqlKvTable : IKvTable
    {
        private MySqlKvConnection m_Connection;
        private string m_Table;

        private const string STMT_SET = "REPLACE INTO `gkv_[TABLE]` SET K = @KEY, V = @VALUE";
        private const string STMT_GET = "SELECT V FROM `gkv_[TABLE]` WHERE K = @KEY LIMIT 1";
        private const string STMT_TRC = "TRUNCATE TABLE `gkv_[TABLE]`";

        /// <summary>
        /// Initialize a new <see cref="MySqlKvTable"/> instance.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Table"></param>
        internal MySqlKvTable(MySqlKvConnection Connection, string Table, bool IsReadOnly)
        {
            (m_Connection = Connection).GrabRef();
            m_Table = Table;

            this.IsReadOnly = IsReadOnly;
        }

        /// <inheritdoc/>
        public bool IsReadOnly { get; }

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return null;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                m_Connection.ThrowIfNotOpened();
                using var Command = m_Connection.MySql.CreateCommand();

                Command.CommandText = STMT_GET.Replace("[TABLE]", m_Table);
                Command.Parameters.Add("KEY", MySqlDbType.String).Value = Key;
                
                try { return await Command.ExecuteScalarAsync(Token) as byte[]; }
                catch
                {
                    m_Connection.ThrowIfNotOpened();
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
            if (IsReadOnly || string.IsNullOrWhiteSpace(Key))
                return false;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                m_Connection.ThrowIfNotOpened();
                using var Command = m_Connection.MySql.CreateCommand();

                Command.CommandText = STMT_SET.Replace("[TABLE]", m_Table);
                Command.Parameters.Add("KEY", MySqlDbType.String).Value = Key;
                Command.Parameters.Add("VALUE", MySqlDbType.LongBlob).Value = Value;

                try { return await Command.ExecuteNonQueryAsync(Token) > 0; }
                catch
                {
                    m_Connection.ThrowIfNotOpened();
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
        /// <returns></returns>
        internal async Task TruncateAsync()
        {
            using var Command = m_Connection.MySql.CreateCommand();
            Command.CommandText = STMT_TRC.Replace("[TABLE]", m_Table);

            try { await Command.ExecuteNonQueryAsync(); }
            catch
            {
            }
        }
    }
}
