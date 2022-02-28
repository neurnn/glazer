using Backrole.Crypto;
using Glazer.Storage.Abstraction;
using Glazer.Transactions.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Glazer.ModelHelpers;

namespace Glazer.Transactions.Memory
{
    /// <summary>
    /// Memory, Volatile Transaction Set.
    /// </summary>
    public class MemoryTransactionSets : ITransactionSets
    {
        private IReadOnlyStorage m_Storage;
        private MemoryTransactionQueue m_WorkingSet, m_PendingSet;

        /// <summary>
        /// Initialize a new <see cref="MemoryTransactionSets"/> instance.
        /// </summary>
        /// <param name="Storage"></param>
        public MemoryTransactionSets(IReadOnlyStorage Storage) => m_Storage = Storage;

        /// <inheritdoc/>
        public ITransactionQueue PendingSet => Locked(this, () => OnDemand(ref m_PendingSet));

        /// <inheritdoc/>
        public ITransactionQueue WorkingSet
        {
            get
            {
                lock(this)
                {
                    if (m_WorkingSet is null || m_WorkingSet.Completed)
                    {
                        Swap(ref m_WorkingSet, ref m_PendingSet);

                        if (m_WorkingSet is null) 
                            m_WorkingSet = new();

                        m_PendingSet?.SetIsWorkingSet(false);
                        m_WorkingSet?.SetIsWorkingSet(true);

                        m_PendingSet?.Clear();
                    }

                    return m_WorkingSet;
                }
            }
        }

        /// <summary>
        /// Export the transaction set to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        public void Export(BinaryWriter Writer)
        {
            lock (this)
            {
                var NoPendings = m_PendingSet is null || m_PendingSet.TotalCount <= 0;
                var NoWorkings = m_WorkingSet is null || m_WorkingSet.TotalCount <= 0;
                var Flags = (byte)(((NoPendings ? 1 : 0) << 1) | ((NoWorkings ? 1 : 0) << 0));

                // Write the flags.
                Writer.Write(Flags);

                if (!NoPendings)
                    m_PendingSet.Export(Writer);

                if (!NoWorkings)
                    m_WorkingSet.Export(Writer);
            }
        }

        /// <summary>
        /// Import the transaction set from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        public void Import(BinaryReader Reader)
        {
            lock(this)
            {
                var Flags = Reader.ReadByte();

                if ((Flags & (1 << 1)) == 0) // Pendings.
                {
                    if (m_PendingSet is null)
                        m_PendingSet = new MemoryTransactionQueue();

                    m_PendingSet.Import(Reader);
                }

                if ((Flags & (1 << 0)) == 0)
                {
                    if (m_WorkingSet is null)
                        m_WorkingSet = new MemoryTransactionQueue();

                    m_WorkingSet.Import(Reader);
                }
            }
        }

        /// <inheritdoc/>
        public TransactionExecutionStatus PeekExecutionStatus(HashValue Id)
        {
            lock(this)
            {
                TransactionExecutionStatus Status;
                
                if (m_PendingSet != null && (Status = m_PendingSet.GetStatus(Id)).Status != TransactionStatus.NotFound)
                    return Status;

                if (m_WorkingSet != null && (Status = m_WorkingSet.GetStatus(Id)).Status != TransactionStatus.NotFound)
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
