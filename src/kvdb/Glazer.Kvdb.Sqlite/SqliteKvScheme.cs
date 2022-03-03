using Glazer.Kvdb.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Sqlite
{
    public class SqliteKvScheme : IKvScheme
    {
        private SqliteKvConnection m_Connection;

        private const string TABLE_CREATE = "CREATE TABLE \"[TABLE]\" (K TEXT PRIMARY KEY NOT NULL, V BLOB NULL DEFAULT (x''))";
        private const string TABLE_EXISTS = "SELECT name FROM sqlite_master WHERE type='table' AND name=@NAME";
        private const string TABLE_LIST = "SELECT name FROM sqlite_master WHERE type='table'";
        private const string TABLE_DROP = "DROP TABLE \"[TABLE]\"";

        /// <summary>
        /// Initialize a new <see cref="SqliteKvScheme"/> instance.
        /// </summary>
        /// <param name="Path"></param>
        public SqliteKvScheme(string Path)
        {
            if (!File.Exists(Path))
                SQLiteConnection.CreateFile(Path);

            var Sqlite = new SQLiteConnection($"Data Source={Path}");
            m_Connection = new SqliteKvConnection(Sqlite.OpenAndReturn());
        }

        /// <summary>
        /// Make the <see cref="SqliteKvScheme"/> file.
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        public static bool MakeFile(string Path)
        {
            try
            {
                if (File.Exists(Path))
                    return false;

                SQLiteConnection.CreateFile(Path);
                return true;
            }

            catch { }
            return false;
        }

        /// <summary>
        /// Test whether the table is exists or not.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<bool> ExistsAsync(string Name, CancellationToken Token = default)
        {
            using var Command = m_Connection.Sqlite.CreateCommand();

            Command.CommandText = TABLE_EXISTS;
            Command.Parameters.Add("NAME", DbType.String).Value = Name;

            try
            {
                var Value = await Command.ExecuteScalarAsync(Token);
                if (Value is not null && ((string)Value).Equals(Name))
                    return true;
            }

            catch
            {
                Token.ThrowIfCancellationRequested();
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<IKvTable> CreateAsync(string Name, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return null;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                if (await ExistsAsync(Name, Token))
                    return null;

                using var Command = m_Connection.Sqlite.CreateCommand();
                Command.CommandText = TABLE_CREATE.Replace("[TABLE]", Name);

                try { await Command.ExecuteNonQueryAsync(Token); }
                catch
                {
                    Token.ThrowIfCancellationRequested();
                }

                if (await ExistsAsync(Name, Token))
                    return new SqliteKvTable(m_Connection, Name);

            }
            finally
            {
                m_Connection.Semaphore.Release();
                m_Connection.DropRef();
            }

            return null;
        }

        /// <inheritdoc/>
        private async Task<List<string>> InternalListAsync(CancellationToken Token)
        {
            var List = new List<string>();

            using var Command = m_Connection.Sqlite.CreateCommand();
            Command.CommandText = TABLE_LIST;

            try
            {
                using var Reader = await Command.ExecuteReaderAsync(Token);

                if (Reader.HasRows)
                {
                    while (await Reader.ReadAsync(Token))
                        List.Add(Reader.GetString(0));
                }
            }

            catch { }
            return List;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<string> ListAsync([EnumeratorCancellation] CancellationToken Token = default)
        {
            List<string> List;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                List = await InternalListAsync(Token);
            }
            finally
            {
                m_Connection.Semaphore.Release();
                m_Connection.DropRef();
            }

            foreach (var Each in List)
                yield return Each;
        }


        /// <inheritdoc/>
        public async Task<IKvTable> OpenAsync(string Name, bool ReadOnly = false, bool Truncate = false, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return null;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                if (await ExistsAsync(Name, Token))
                {
                    if (Truncate)
                    {
                        if (ReadOnly)
                            throw new InvalidOperationException("the read-only and truncate options cannot be set both.");

                        var Table = new SqliteKvTable(m_Connection, Name);
                        await Table.TruncateAsync();
                        return Table;
                    }

                    return new SqliteKvTable(m_Connection, Name);
                }
            }

            finally
            {
                m_Connection.Semaphore.Release();
                m_Connection.DropRef();
            }

            return null;
        }

        /// <summary>
        /// Drop the table asynchronously.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task InternalDropAsync(string Name, CancellationToken Token)
        {
            using var Command = m_Connection.Sqlite.CreateCommand();
            Command.CommandText = TABLE_DROP.Replace("[TABLE]", Name);

            try { await Command.ExecuteNonQueryAsync(Token); }
            catch
            {
                Token.ThrowIfCancellationRequested();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DropAsync(string Name, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                await InternalDropAsync(Name, Token);

                if (await ExistsAsync(Name, Token))
                    return false;

                return true;
            }
            finally
            {
                m_Connection.Semaphore.Release();
                m_Connection.DropRef();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DropAsync(CancellationToken Token = default)
        {
            m_Connection.GrabRef();
            await m_Connection.Semaphore.WaitAsync();

            try
            {
                while (true)
                {
                    var List = await InternalListAsync(Token);
                    if (List.Count <= 0)
                        break;

                    foreach (var Each in List)
                    {
                        Token.ThrowIfCancellationRequested();
                        await InternalDropAsync(Each, Token);
                    }
                }
            }
            finally
            {
                m_Connection.Semaphore.Release();
                m_Connection.DropRef();
            }

            return true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            m_Connection.DropRef();
        }
    }
}
