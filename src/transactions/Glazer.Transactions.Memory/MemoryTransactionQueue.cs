using Backrole.Crypto;
using Glazer.Common.Models;
using Glazer.Transactions.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Glazer.Transactions.Memory
{
    public class MemoryTransactionQueue : ITransactionQueue, IEnumerable<KeyValuePair<HashValue, TransactionExecutionStatus>>
    {
        private List<TransactionRegistration> m_Pendings = new();
        private Dictionary<HashValue, TransactionExecutionStatus> m_Status = new();
        private bool m_HasChanges = false;

        /// <inheritdoc/>
        public int TotalCount => ModelHelpers.Locked(this, () => m_Pendings.Count + m_Status.Count);

        /// <inheritdoc/>
        public int Pendings => ModelHelpers.Locked(this, () => m_Pendings.Count);

        /// <inheritdoc/>
        public bool IsWorkingSet { get; private set; }

        /// <inheritdoc/>
        public bool Completed
        {
            get
            {
                lock(this)
                {
                    if (m_Pendings.Count > 0)
                        return false;

                    var Count = 0;

                    foreach (var Each in m_Status)
                    {
                        if (Each.Value.IsCompletedAnyway)
                            Count++;
                    }

                    return Count >= m_Status.Count;
                }
            }
        }

        /// <summary>
        /// Indicates whether the queue has changes or not.
        /// </summary>
        public bool HasChanges => ModelHelpers.Locked(this, () => m_HasChanges);

        /// <inheritdoc/>
        public bool Clear()
        {
            lock (this)
            {
                if (IsWorkingSet)
                    return false;

                m_Status.Clear();
                m_Pendings.Clear();
                m_HasChanges = false;
                return true;
            }
        }

        /// <summary>
        /// Set the <see cref="IsWorkingSet"/> value.
        /// </summary>
        /// <param name="Value"></param>
        public void SetIsWorkingSet(bool Value)
        {
            lock(this)
            {
                IsWorkingSet = Value;
            }
        }

        /// <summary>
        /// Set the <see cref="HasChanges"/> value.
        /// </summary>
        /// <param name="Value"></param>
        public void SetHasChanges(bool Value)
        {
            lock(this)
            {
                m_HasChanges = Value;
            }
        }

        /// <summary>
        /// Export the <see cref="MemoryTransactionQueue"/> to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        public void Export(BinaryWriter Writer)
        {
            lock(this)
            {
                Writer.Write7BitEncodedInt(m_Pendings.Count);
                Writer.Write7BitEncodedInt(m_Status.Count);

                foreach(var Each in m_Pendings)
                    Each.Request.Export(Writer);

                foreach(var Each in m_Status)
                {
                    Writer.Write(Each.Key);
                    Writer.Write7BitEncodedInt((int)Each.Value.Status);
                    Writer.Write(Each.Value.Reason ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Import the <see cref="MemoryTransactionQueue"/> from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        public void Import(BinaryReader Reader)
        {
            lock(this)
            {
                var Changes = m_HasChanges;
                var Requests = Reader.Read7BitEncodedInt();
                var Status = Reader.Read7BitEncodedInt();
                var Request = null as TransactionRequest;

                for (var i = 0; i < Requests; ++i)
                {
                    (Request = new()).Import(Reader);
                    Enqueue(Request);
                }

                for(var i = 0; i < Status; ++i)
                {
                    var Id = Reader.ReadHashValue();
                    var Value = (TransactionStatus) Reader.Read7BitEncodedInt();
                    var Reason = Reader.ReadString();

                    m_Status[Id] = new TransactionExecutionStatus(Value, Reason);
                }

                m_HasChanges = Changes;
            }
        }

        /// <inheritdoc/>
        public bool Contains(HashValue Id)
        {
            lock (this)
            {
                if (m_Pendings.FindIndex(X => X.Id == Id) >= 0)
                    return true;

                if (m_Status.ContainsKey(Id))
                    return true;

                return false;
            }
        }

        /// <inheritdoc/>
        public TransactionRegistration Enqueue(TransactionRequest Request)
        {
            lock (this)
            {
                if (IsWorkingSet)
                    throw new InvalidOperationException("the queue is read-only.");

                if (Request.Signature.IsValid)
                {
                    var Registration = new TransactionRegistration(Request, TransactionStatus.Queued);
                    if (Contains(Registration.Id))
                        return new TransactionRegistration(Registration.Id, Request, TransactionStatus.Already);

                    m_Pendings.Add(Registration);
                    m_HasChanges = true;

                    return Registration;
                }
            }

            return new TransactionRegistration(Request, TransactionStatus.SignatureError);
        }

        /// <inheritdoc/>
        public TransactionExecutionStatus GetStatus(HashValue Id)
        {
            lock(this)
            {
                if (m_Status.TryGetValue(Id, out var Status))
                    return Status;

                if (m_Pendings.FindIndex(X => X.Id == Id) >= 0)
                    return new TransactionExecutionStatus(TransactionStatus.Queued, string.Empty);

                return new TransactionExecutionStatus(TransactionStatus.NotFound, string.Empty);
            }
        }

        /// <inheritdoc/>
        public ITransactionQueue GetPendings(IList<TransactionRegistration> Requests)
        {
            lock(this)
            {
                foreach (var Registration in m_Pendings)
                    Requests.Add(Registration);
            }

            return this;
        }

        /// <summary>
        /// Get all status.
        /// </summary>
        /// <param name="Status"></param>
        /// <returns></returns>
        public ITransactionQueue GetStatus(IDictionary<HashValue, TransactionExecutionStatus> Status)
        {
            lock (this)
            {
                foreach (var Each in m_Pendings)
                    Status[Each.Id] = new TransactionExecutionStatus(Each.Status, string.Empty);

                foreach (var Each in m_Status)
                    Status[Each.Key] = Each.Value;
            }

            return this;
        }

        /// <inheritdoc/>
        public ITransactionQueue SetStatus(HashValue Id, TransactionExecutionStatus Status)
        {
            lock (this)
            {
                if (m_Status.TryGetValue(Id, out var CurrentStatus))
                {
                    if (CurrentStatus.Status != Status.Status &&
                        CurrentStatus.Reason != Status.Reason)
                    {
                        m_Status[Id] = Status;
                        m_HasChanges = true;
                    }
                }

                else
                {
                    var Index = m_Pendings.FindIndex(X => X.Id == Id);
                    if (Index >= 0)
                    {
                        m_Status[Id] = Status;
                        m_Pendings.RemoveAt(Index);
                        m_HasChanges = true;
                    }
                }
            }

            return this;
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<HashValue, TransactionExecutionStatus>> GetEnumerator()
        {
            var Temp = new Dictionary<HashValue, TransactionExecutionStatus>();

            GetStatus(Temp);

            return Temp.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
