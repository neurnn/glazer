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
    public struct MemoryKvReadOnlyTable : IKvTable, IEnumerable<KeyValuePair<string, byte[]>>
    {
        private static readonly IEnumerable<KeyValuePair<string, byte[]>> EMPTY_KV = new KeyValuePair<string, byte[]>[0];
        private static readonly Task<byte[]> TASK_NULL = Task.FromResult(null as byte[]);
        private static readonly Task<bool> TASK_FALSE = Task.FromResult(false);
        
        private IReadOnlyDictionary<string, byte[]> m_KeyValues;

        /// <summary>
        /// Initialize a new <see cref="MemoryKvReadOnlyTable"/> instance.
        /// </summary>
        /// <param name="Blobs"></param>
        public MemoryKvReadOnlyTable(IReadOnlyDictionary<string, byte[]> Blobs)
        {
            m_KeyValues = Blobs;
        }

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <summary>
        /// Copy values to <see cref="MemoryKvTable"/>.
        /// </summary>
        /// <param name="Table"></param>
        public void CopyTo(MemoryKvTable Table)
        {
            foreach (var Each in m_KeyValues)
                Table.InternalSet(Each.Key, Each.Value);
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
            foreach (var Each in m_KeyValues)
            {
                if (Each.Value is not null)
                    Json[Each.Key] = Convert.ToBase64String(Each.Value);

                else
                    Json[Each.Key] = null;
            }
        }

        /// <summary>
        /// Export KV table to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        public void Export(BinaryWriter Writer)
        {
            if (m_KeyValues is null)
            {
                Writer.Write7BitEncodedInt(0);
                return;
            }

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

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string Key, CancellationToken Token = default)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return TASK_NULL;

            if (m_KeyValues.TryGetValue(Key, out var Value))
                return Task.FromResult(Value);

            return TASK_NULL;
        }

        /// <inheritdoc/>
        public Task<bool> SetAsync(string Key, byte[] Value, CancellationToken Token = default) => TASK_FALSE;

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator()
        {
            return m_KeyValues != null ? m_KeyValues.GetEnumerator() : EMPTY_KV.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_KeyValues != null ? m_KeyValues.GetEnumerator() : EMPTY_KV.GetEnumerator();
        }
    }
}
