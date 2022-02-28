using Glazer.Kvdb.Abstractions;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.MySQL
{
    public class MySqlKvScheme : IKvScheme
    {
        private MySqlKvConnection m_Connection;
        private string m_Scheme;

        private const string TABLE_CREATE = "CREATE TABLE `gkv_[TABLE]` (`K` VARCHAR(128) NOT NULL, `V` LONGBLOB NULL, PRIMARY KEY (`K`))";
        private const string TABLE_EXISTS = "SELECT table_name FROM information_schema.tables WHERE table_schema = @SCHEME AND table_name = @NAME LIMIT 1";
        private const string TABLE_LIST = "SELECT table_name FROM information_schema.tables WHERE table_schema = @SCHEME AND table_name LIKE 'gkv_%'";
        private const string TABLE_DROP = "DROP TABLE `gkv_[TABLE]`";

        /// <summary>
        /// Initialize a new <see cref="MySqlKvConnection"/> instance.
        /// </summary>
        /// <param name="Path"></param>
        public MySqlKvScheme(string User, string Pass, string Scheme, string Host, int Port = 3306)
        {
            bool Succeed = false;
            var ConnString = MakeConnectionString(User, Pass, Scheme, Host, Port);
            var MySql = new MySqlConnection(ConnString);

            try
            {
                try { MySql.Open(); }
                catch(Exception e)
                {
                    throw new KvdbUnavailableException("the connection couldn't be established.", e);
                }

                Succeed = true;
                m_Scheme = Scheme;
                m_Connection = new MySqlKvConnection(MySql, ConnString);
            }
            finally
            {
                if (!Succeed)
                {
                    try { MySql.Dispose(); }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Make the connection string.
        /// </summary>
        /// <param name="User"></param>
        /// <param name="Pass"></param>
        /// <param name="Scheme"></param>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        private static string MakeConnectionString(string User, string Pass, string Scheme, string Host, int Port)
        {
            var Conn = new MySqlConnectionStringBuilder();

            Conn.Server = Host;
            Conn.Port = (uint)Port;
            Conn.UserID = User;
            Conn.Password = Pass;
            Conn.Database = Scheme;
            Conn.CharacterSet = "utf8";

            return Conn.ToString();
        }

        /// <summary>
        /// Test whether the table is exists or not.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<bool> ExistsAsync(string Name, CancellationToken Token = default)
        {
            using var Command = m_Connection.MySql.CreateCommand();

            Command.CommandText = TABLE_EXISTS;
            Command.Parameters.Add("SCHEME", MySqlDbType.String).Value = m_Scheme;
            Command.Parameters.Add("NAME", MySqlDbType.String).Value = $"gkv_{Name}";

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
                m_Connection.ThrowIfNotOpened();

                if (await ExistsAsync(Name, Token))
                    return null;

                using var Command = m_Connection.MySql.CreateCommand();
                Command.CommandText = TABLE_CREATE.Replace("[TABLE]", Name);

                try { await Command.ExecuteNonQueryAsync(Token); }
                catch
                {
                    m_Connection.ThrowIfNotOpened();
                    Token.ThrowIfCancellationRequested();
                }

                if (await ExistsAsync(Name, Token))
                    return new MySqlKvTable(m_Connection, Name, false);

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

            using var Command = m_Connection.MySql.CreateCommand();
            Command.CommandText = TABLE_LIST;

            try
            {
                using var Reader = await Command.ExecuteReaderAsync(Token);

                if (Reader.HasRows)
                {
                    while (await Reader.ReadAsync(Token))
                    {
                        var Name = Reader.GetString(0);

                        if (string.IsNullOrWhiteSpace(Name))
                            continue;

                        if (Name.StartsWith("gkv_"))
                            List.Add(Name.Substring(4));
                    }
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
                m_Connection.ThrowIfNotOpened();
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
                m_Connection.ThrowIfNotOpened();
                if (await ExistsAsync(Name, Token))
                {
                    if (Truncate)
                    {
                        if (ReadOnly)
                            throw new InvalidOperationException("the read-only and truncate options cannot be set both.");

                        var Table = new MySqlKvTable(m_Connection, Name, ReadOnly);
                        await Table.TruncateAsync();
                        return Table;
                    }

                    return new MySqlKvTable(m_Connection, Name, ReadOnly);
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
            using var Command = m_Connection.MySql.CreateCommand();
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
                m_Connection.ThrowIfNotOpened();
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
                m_Connection.ThrowIfNotOpened();
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
