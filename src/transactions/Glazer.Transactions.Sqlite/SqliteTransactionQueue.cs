using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Models;
using Glazer.Transactions.Abstractions;
using Glazer.Transactions.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Transactions.Sqlite
{
    public class SqliteTransactionQueue : ITransactionQueue, IDisposable
    {
        private static readonly byte[] EMPTY_TRS = new byte[0];

        private MemoryTransactionQueue m_Cache;
        private SQLiteConnection m_Sqlite;

        private const string STMT_INIT =
            "CREATE TABLE IF NOT EXISTS \"glt_transactions\" " +
            "(TID TEXT PRIMARY KEY NOT NULL, TRX BLOB NULL DEFAULT(x''), TST INTEGER DEFAULT 0, TRS BLOB NULL DEFAULT(x''))";

        private const string STMT_RESTORE =
            "SELECT TID, TRX, TST, TRS FROM \"glt_transactions\" WHERE TST > 0";

        private const string STMT_CLEAR = "DELETE FROM \"glt_transactions\"";

        private const string STMT_SET_ITEM =
            "INSERT INTO \"glt_transactions\" (TID, TRX, TST, TRS) VALUES (@TID, @TRX, @TST, @TRS)";

        private const string STMT_SET_STATUS =
            "UPDATE \"glt_transactions\" SET TST = @TST, TRS = @TRS, TRX = null WHERE TID = @TID";

        /// <summary>
        /// Initialize a new <see cref="SqliteTransactionQueue"/> instance.
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="IsWorkingSet"></param>
        public SqliteTransactionQueue(string Path)
        {
            if (!File.Exists(this.Path = Path))
                SQLiteConnection.CreateFile(Path);
            
            m_Sqlite = new SQLiteConnection($"Data Source={Path}");
            using (var Stmt = m_Sqlite.CreateCommand())
            {
                Stmt.CommandText = STMT_INIT;
                try   { Stmt.ExecuteNonQuery(); }
                catch { }
            }

            Restore(); // --> Restore the transaction informations from. 
        }

        /// <summary>
        /// Load the datas from <see cref="SQLiteConnection"/>.
        /// </summary>
        private void Restore()
        {
            using (var Stmt = m_Sqlite.CreateCommand())
            {
                Stmt.CommandText = STMT_RESTORE;

                using (var Reader = Stmt.ExecuteReader())
                {
                    if (!Reader.HasRows)
                        return;

                    while (Reader.Read())
                    {
                        var TID = Reader.GetString(0);
                        var TRX = Reader.GetValue(1) as byte[];
                        var TST = (TransactionStatus) Reader.GetInt32(2);
                        var TRS = Reader.GetValue(3) as byte[] ?? EMPTY_TRS;

                        if (TST == TransactionStatus.NotFound)
                            continue;

                        if (TST == TransactionStatus.Queued)
                        {
                            if (TRX is null || TRX.Length <= 0)
                                continue;

                            using var DataReader = new PacketReader(TRX);
                            var Request = new TransactionRequest();

                            Request.Import(DataReader);
                            m_Cache.Enqueue(Request);
                        }

                        else if (HashValue.TryParse(TID, out var Id))
                        {
                            var Reason = TRS.Length > 0 ? Encoding.UTF8.GetString(TRS) : string.Empty;
                            m_Cache.SetStatus(Id, new TransactionExecutionStatus(TST, Reason));
                        }
                    }
                }

                m_Cache.SetHasChanges(false);
            }
        }

        /// <summary>
        /// Path to sqlite database file.
        /// </summary>
        public string Path { get; }

        /// <inheritdoc/>
        public int TotalCount => m_Cache.TotalCount;

        /// <inheritdoc/>
        public int Pendings => m_Cache.Pendings;

        /// <inheritdoc/>
        public bool IsWorkingSet => m_Cache.IsWorkingSet;

        /// <summary>
        /// Indicates whether the queue has changes or not.
        /// </summary>
        public bool HasChanges => m_Cache.HasChanges;

        /// <inheritdoc/>
        public bool Completed => m_Cache.Completed;

        /// <inheritdoc/>
        public bool Clear()
        {
            lock (this)
            {
                if (IsWorkingSet || m_Sqlite is null)
                    return false;

                using (var Stmt = m_Sqlite.CreateCommand())
                {
                    Stmt.CommandText = STMT_CLEAR;
                    try { Stmt.ExecuteNonQuery(); }
                    catch { return false; }
                }

                m_Cache.Clear();
                return true;
            }
        }

        /// <summary>
        /// Set <see cref="IsWorkingSet"/> value.
        /// </summary>
        /// <param name="Value"></param>
        public void SetIsWorkingSet(bool Value) => m_Cache.SetIsWorkingSet(Value);

        /// <summary>
        /// Set <see cref="HasChanges"/> value.
        /// </summary>
        /// <param name="Value"></param>
        internal void SetHasChanges(bool Value) => m_Cache.SetHasChanges(Value);

        /// <inheritdoc/>
        public bool Contains(HashValue Id) => m_Cache.Contains(Id);

        /// <inheritdoc/>
        public TransactionRegistration Enqueue(TransactionRequest Request)
        {
            lock(this)
            {
                if (m_Sqlite is null)
                    throw new ObjectDisposedException(nameof(SqliteTransactionQueue));

                if (m_Cache.IsWorkingSet)
                    throw new InvalidOperationException("the queue is read-only.");

                if (Request.Signature.IsValid)
                {
                    var Registration = new TransactionRegistration(Request, TransactionStatus.Queued);
                    if (Contains(Registration.Id))
                        return new TransactionRegistration(Registration.Id, Request, TransactionStatus.Already);

                    try
                    {
                        using var Writer = new PacketWriter();
                        Request.Export(Writer);

                        using var Command = m_Sqlite.CreateCommand();
                        Command.CommandText = STMT_SET_ITEM;
                        Command.Parameters.Add("TID", DbType.String).Value = Registration.Id.ToString();
                        Command.Parameters.Add("TRX", DbType.Binary).Value = Writer.ToByteArray();
                        Command.Parameters.Add("TST", DbType.Int32).Value = (int)TransactionStatus.Queued;
                        Command.Parameters.Add("TRS", DbType.Binary).Value = EMPTY_TRS;

                        if (Command.ExecuteNonQuery() > 0)
                            return m_Cache.Enqueue(Request);
                    }
                    catch
                    {
                    }

                    return new TransactionRegistration(Request, TransactionStatus.QueueError);
                }
            }

            return new TransactionRegistration(Request, TransactionStatus.SignatureError);
        }

        /// <inheritdoc/>
        public TransactionExecutionStatus GetStatus(HashValue Id)
        {
            lock(this)
            {
                return m_Cache.GetStatus(Id);
            }
        }

        /// <inheritdoc/>
        public ITransactionQueue GetPendings(IList<TransactionRegistration> Requests)
        {
            lock(this)
            {
                m_Cache.GetPendings(Requests);
            }

            return this;
        }

        /// <inheritdoc/>
        public ITransactionQueue GetStatus(IDictionary<HashValue, TransactionExecutionStatus> Status)
        {
            lock(this)
            {
                return m_Cache.GetStatus(Status);
            }
        }

        /// <inheritdoc/>
        public ITransactionQueue SetStatus(HashValue Id, TransactionExecutionStatus Status)
        {
            lock(this)
            {
                using var Command = m_Sqlite.CreateCommand();
                Command.CommandText = STMT_SET_STATUS;

                Command.Parameters.Add("TID", DbType.String).Value = Id.ToString();
                Command.Parameters.Add("TST", DbType.Int32).Value = (int)Status.Status;
                Command.Parameters.Add("TRS", DbType.Binary).Value = !string.IsNullOrWhiteSpace(Status.Reason)
                    ? Encoding.UTF8.GetBytes(Status.Reason) : EMPTY_TRS;

                try
                {
                    if (Command.ExecuteNonQuery() > 0)
                        m_Cache.SetStatus(Id, Status);
                }

                catch
                {
                }

                return this;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (this)
            {
                if (m_Sqlite is null)
                    return;

                m_Cache.Clear();
                m_Sqlite.Dispose();

                m_Sqlite = null;
            }
        }

    }
}
