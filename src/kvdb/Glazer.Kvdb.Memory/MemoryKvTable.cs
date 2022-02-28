using Glazer.Kvdb.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Memory
{
    public class MemoryKvTable : IKvTable, IEnumerable<KeyValuePair<string, byte[]>>
    {
        private static readonly Task<byte[]> TASK_NULL = Task.FromResult(null as byte[]);
        private static readonly Task<bool> TASK_TRUE = Task.FromResult(true);
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);

        private Dictionary<string, byte[]> m_KeyValues = new();

        /// <summary>
        /// Internal Set.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        internal void InternalSet(string Key, byte[] Value)
        {
            lock(m_KeyValues)
            {
                m_KeyValues[Key] = Value;
            }
        }

        /// <summary>
        /// Convert the <see cref="MemoryKvTable"/> to the <see cref="MemoryKvReadOnlyTable"/>.
        /// </summary>
        /// <returns></returns>
        public MemoryKvReadOnlyTable ToReadOnly()
            => new MemoryKvReadOnlyTable(new Dictionary<string, byte[]>(m_KeyValues));

        /// <summary>
        /// Import KV table from stream.
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="LeaveOpen"></param>
        public void Import(Stream Stream, bool LeaveOpen = true)
        {
            using (var Reader = new BinaryReader(Stream, Encoding.UTF8, LeaveOpen))
            {
                Import(Reader);
            }
        }

        /// <summary>
        /// Import KV table from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        public void Import(BinaryReader Reader)
        {
            lock (m_KeyValues)
            {
                var Count = Reader.Read7BitEncodedInt();
                for (var i = 0; i < Count; ++i)
                {
                    var Key = Reader.ReadString();

                    if (Reader.ReadByte() != byte.MinValue)
                    {
                        var Length = Reader.Read7BitEncodedInt();
                        m_KeyValues[Key] = Reader.ReadBytes(Length);
                    }

                    else
                        m_KeyValues[Key] = null;
                }
            }
        }

        /// <summary>
        /// Import KV table from <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        public void Import(JObject Json)
        {
            lock (m_KeyValues)
            {
                foreach (var Each in Json.Properties())
                {
                    if (Each.Value.Type == JTokenType.String)
                        m_KeyValues[Each.Name] = Convert.FromBase64String(Each.Value.Value<string>());

                    else if (Each.Value.Type == JTokenType.Null)
                        m_KeyValues[Each.Name] = null;
                }
            }
        }

        /// <summary>
        /// Export KV table to stream.
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
        /// Export the KV table to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        public void Export(JObject Json)
        {
            lock (m_KeyValues)
            {
                foreach (var Each in m_KeyValues)
                {
                    if (Each.Value is not null)
                        Json[Each.Key] = Convert.ToBase64String(Each.Value);

                    else
                        Json[Each.Key] = null;
                }
            }
        }

        /// <summary>
        /// Export KV table to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        public void Export(BinaryWriter Writer)
        {
            lock (m_KeyValues)
            {
                Writer.Write7BitEncodedInt(m_KeyValues.Count);
                foreach (var Each in m_KeyValues)
                {
                    Writer.Write(Each.Key);

                    if (Each.Value is null)
                    {
                        Writer.Write(byte.MinValue);
                        continue;
                    }

                    Writer.Write(byte.MaxValue);
                    Writer.Write7BitEncodedInt(Each.Value.Length);
                    Writer.Write(Each.Value);
                }
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly { get; } = false;

        /// <summary>
        /// Truncate the table.
        /// </summary>
        internal void Truncate()
        {
            lock (m_KeyValues)
                  m_KeyValues.Clear();
        }

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            lock (m_KeyValues)
            {
                if (string.IsNullOrWhiteSpace(Key))
                    return TASK_NULL;

                if (m_KeyValues.TryGetValue(Key, out var Value))
                    return Task.FromResult(Value);

                return TASK_NULL;
            }
        }

        /// <inheritdoc/>
        public Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default)
        {
            lock (m_KeyValues)
            {
                if (string.IsNullOrWhiteSpace(Key))
                    return TASK_FALSE;

                m_KeyValues[Key] = Value;
                return TASK_TRUE;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator() => m_KeyValues.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => m_KeyValues.GetEnumerator();
    }
}
