﻿using Backrole.Crypto;
using Glazer.Storage.Abstraction;
using Glazer.Transactions.Abstractions;
using Glazer.Transactions.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Transactions.Sqlite
{
    /// <summary>
    /// Sqlite, Non-volatile Transaction Set.
    /// </summary>
    public class SqliteTransactionSets : ITransactionSets
    {
        private IReadOnlyStorage m_Storage;
        private string m_DataDir;

        private SqliteTransactionQueue m_PendingSet;
        private SqliteTransactionQueue m_WorkingSet;

        private Task m_Verification;

        /// <summary>
        /// Initialize a new <see cref="SqliteTransactionSets"/> instance.
        /// </summary>
        /// <param name="Storage"></param>
        /// <param name="FilePath"></param>
        public SqliteTransactionSets(IReadOnlyStorage Storage, string DataDir)
        {
            if (!Directory.Exists(DataDir))
                 Directory.CreateDirectory(DataDir);

            m_DataDir = DataDir;
            m_Storage = Storage;

            m_PendingSet = new SqliteTransactionQueue(Path.Combine(m_DataDir, "pendingset.db"));
            m_WorkingSet = new SqliteTransactionQueue(Path.Combine(m_DataDir, "workingset.db"));
        }

        /// <inheritdoc/>
        public ITransactionQueue PendingSet
        {
            get
            {
                Initialize().GetAwaiter().GetResult();
                return ModelHelpers.Locked(this, () => m_PendingSet);
            }
        }

        /// <inheritdoc/>
        public ITransactionQueue WorkingSet
        {
            get
            {
                Initialize().GetAwaiter().GetResult();
                lock (this)
                {
                    if (m_WorkingSet.Completed && m_PendingSet.Pendings > 0)
                    {
                        var Pendings = new List<TransactionRegistration>();
                        m_PendingSet.GetPendings(Pendings);

                        m_WorkingSet.SetIsWorkingSet(false);
                        m_WorkingSet.Clear();

                        foreach (var Each in Pendings)
                        {
                            if (Each.Status != TransactionStatus.Queued)
                                continue;

                            m_WorkingSet.Enqueue(Each.Request);
                        }

                        m_WorkingSet.SetIsWorkingSet(true);
                        m_WorkingSet.SetHasChanges(false);
                    }

                    return m_WorkingSet;
                }
            }
        }

        /// <summary>
        /// Verify the <see cref="SqliteTransactionSets"/> status.
        /// </summary>
        /// <returns></returns>
        private async Task VerifyAsync()
        {
            var Pendings = new List<TransactionRegistration>();
            m_PendingSet.GetPendings(Pendings);

            foreach (var Each in Pendings)
            {
                var Status = m_WorkingSet.GetStatus(Each.Id);
                if (Status.Status != TransactionStatus.NotFound)
                {
                    m_PendingSet.SetStatus(Each.Id, Status);
                    continue;
                }

                var BlockId = await m_Storage.GetTransactionAsync(Each.Id);
                if (BlockId.IsValid)
                {
                    m_PendingSet.SetStatus(Each.Id,
                        new TransactionExecutionStatus(TransactionStatus.Completed, string.Empty));
                }
            }

            if (m_PendingSet.Pendings <= 0)
                m_PendingSet.Clear();

            Pendings.Clear();
            m_WorkingSet.GetPendings(Pendings);

            foreach (var Each in Pendings)
            {
                var BlockId = await m_Storage.GetTransactionAsync(Each.Id);
                if (BlockId.IsValid)
                {
                    m_WorkingSet.SetStatus(Each.Id,
                        new TransactionExecutionStatus(TransactionStatus.Completed, string.Empty));
                }
            }

            if (m_WorkingSet.Pendings <= 0)
                m_WorkingSet.Clear();
        }

        /// <summary>
        /// Initialize the <see cref="SqliteTransactionSets"/> once.
        /// </summary>
        /// <returns></returns>
        private Task Initialize()
        {
            lock(this)
            {
                if (m_Verification is null)
                    m_Verification = VerifyAsync();

                return m_Verification;
            }
        }

        /// <inheritdoc/>
        public TransactionExecutionStatus PeekExecutionStatus(HashValue Id)
        {
            Initialize().GetAwaiter().GetResult();

            lock (this)
            {
                TransactionExecutionStatus Status;

                if ((Status = m_PendingSet.GetStatus(Id)).Status != TransactionStatus.NotFound)
                    return Status;

                if ((Status = m_WorkingSet.GetStatus(Id)).Status != TransactionStatus.NotFound)
                    return Status;
            }

            var BlockId = m_Storage.GetTransactionAsync(Id).GetAwaiter().GetResult();
            if (BlockId.IsValid)
            {
                return new TransactionExecutionStatus(TransactionStatus.Completed, string.Empty);
            }

            return new TransactionExecutionStatus(TransactionStatus.NotFound, string.Empty);
        }

        /// <inheritdoc/>
        public async Task<TransactionExecutionStatus> WaitExecutionStatusAsync(HashValue Id, CancellationToken Token = default)
        {
            await Initialize();

            while (true)
            {
                var Status = PeekExecutionStatus(Id);
                if (Status.IsCompletedAnyway)
                {
                    return Status;
                }

                if (Token.IsCancellationRequested)
                    break; /* Retried but not completed. */

                try { await Task.Delay(100, Token); }
                catch
                {
                    /* Retry again. */
                    continue;
                }
            }

            Token.ThrowIfCancellationRequested();
            return new TransactionExecutionStatus(TransactionStatus.NotFound, string.Empty);
        }
    }
}
