using Glazer.Kvdb.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Memory
{
    public class MemoryKvScheme : IKvScheme
    {
        private static readonly Task<IKvTable> TASK_NULL_TABLE = Task.FromResult(null as IKvTable);
        private static readonly Task<bool> TASK_TRUE = Task.FromResult(true);
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);

        private Dictionary<string, MemoryKvTable> m_Tables = new();

        /// <summary>
        /// Import KV scheme from stream.
        /// </summary>
        /// <param name="Stream"></param>
        /// <returns></returns>
        public void Import(Stream Stream, bool LeaveOpen = true)
        {
            using (var Reader = new BinaryReader(Stream, Encoding.UTF8, LeaveOpen))
            {
                Import(Reader);
            }
        }

        /// <summary>
        /// Import KV scheme from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        public void Import(BinaryReader Reader)
        {
            lock (m_Tables)
            {
                var Count = Reader.Read7BitEncodedInt();

                for (var i = 0; i < Count; ++i)
                {
                    var Name = Reader.ReadString();

                    if (!m_Tables.TryGetValue(Name, out var Table))
                        m_Tables[Name] = Table = new MemoryKvTable();

                    Table.Import(Reader);
                }
            }
        }

        /// <summary>
        /// Import KV scheme from <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        public void Import(JObject Json)
        {
            lock (m_Tables)
            {
                foreach (var Each in Json.Properties())
                {
                    if (Each.Value.Type == JTokenType.Object)
                    {
                        var Name = Each.Name;

                        if (!m_Tables.TryGetValue(Name, out var Table))
                            m_Tables[Name] = Table = new MemoryKvTable();

                        Table.Import(Each.Value<JObject>());
                    }
                }
            }
        }

        /// <summary>
        /// Export KV scheme to stream.
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="LeaveOpen"></param>
        public void Export(Stream Stream, bool LeaveOpen = true)
        {
            using (var Writer = new BinaryWriter(Stream, Encoding.UTF8, LeaveOpen))
            {
                Export(Writer);
            }
        }

        /// <summary>
        /// Export KV scheme to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        public void Export(BinaryWriter Writer)
        {
            lock (m_Tables)
            {
                Writer.Write7BitEncodedInt(m_Tables.Count);

                foreach (var Each in m_Tables)
                {
                    Writer.Write(Each.Key);
                    Each.Value.Export(Writer);
                }
            }
        }

        /// <summary>
        /// Export the KV table to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        public void Export(JObject Json)
        {
            lock (m_Tables)
            {
                foreach (var Each in m_Tables)
                {
                    var Table = new JObject();
                    Each.Value.Export(Table);
                    Json[Each.Key] = Table;
                }
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<string> ListAsync([EnumeratorCancellation] CancellationToken Token = default)
        {
            lock (m_Tables)
            {
                foreach (var Key in m_Tables.Keys)
                    yield return Key;
            }

            await TASK_TRUE;
        }

        /// <inheritdoc/>
        public Task<IKvTable> CreateAsync(string Name, CancellationToken Token = default)
        {
            lock (m_Tables)
            {
                if (string.IsNullOrWhiteSpace(Name) || m_Tables.ContainsKey(Name))
                    return TASK_NULL_TABLE;

                return Task.FromResult<IKvTable>(m_Tables[Name] = new MemoryKvTable());
            }
        }

        /// <inheritdoc/>
        public Task<IKvTable> OpenAsync(string Name, bool ReadOnly = false, bool Truncate = false, CancellationToken Token = default)
        {
            lock(m_Tables)
            {
                if (string.IsNullOrWhiteSpace(Name) || !m_Tables.TryGetValue(Name, out var Table))
                    return TASK_NULL_TABLE;

                if (ReadOnly)
                    return Task.FromResult<IKvTable>(new MemoryKvTableView(Table));

                if (Truncate)
                {
                    if (ReadOnly)
                        throw new InvalidOperationException("the read-only and truncate options cannot be set both.");

                    Table.Truncate();
                }

                return Task.FromResult<IKvTable>(Table);
            }
        }

        /// <inheritdoc/>
        public Task<bool> DropAsync(string Name, CancellationToken Token = default)
        {
            lock (m_Tables)
            {
                if (string.IsNullOrWhiteSpace(Name) || !m_Tables.Remove(Name))
                    return TASK_FALSE;

                return TASK_TRUE;
            }
        }

        /// <inheritdoc/>
        public Task<bool> DropAsync(CancellationToken Token = default) => TASK_FALSE;

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
