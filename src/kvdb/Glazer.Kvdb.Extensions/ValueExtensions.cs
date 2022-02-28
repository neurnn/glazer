using Glazer.Kvdb.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Kvdb.Extensions
{
    public static class ValueExtensions
    {
        /// <summary>
        /// Get the value as <see cref="Guid"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<Guid> GetGuidAsync(this IKvTable Table, string Key, CancellationToken Token = default)
        {
            var Value = await Table.GetAsync(Key, Token);
            if (Value is null || Value.Length != 16)
                return Guid.Empty;

            return new Guid(Value);
        }

        /// <summary>
        /// Set the value as <see cref="Guid"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static Task<bool> SetGuidAsync(this IKvTable Table, string Key, Guid Value, CancellationToken Token = default)
        {
            return Table.SetAsync(Key, Value.ToByteArray(), Token);
        }

        /// <summary>
        /// Get the value as <see cref="Guid"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static Guid GetGuid(this IKvTable Table, string Key) => Table.GetGuidAsync(Key).GetAwaiter().GetResult();

        /// <summary>
        /// Set the value as <see cref="Guid"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool SetGuid(this IKvTable Table, string Key, Guid Value) => Table.SetGuidAsync(Key, Value).GetAwaiter().GetResult();

        /// <summary>
        /// Get the value as string by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<string> GetStringAsync(this IKvTable Table, string Key, CancellationToken Token = default)
        {
            var Blob = await Table.GetAsync(Key, Token);
            return Blob != null
                ? Encoding.UTF8.GetString(Blob) 
                : null;
        }

        /// <summary>
        /// Get the value as string by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static string GetString(this IKvTable Table, string Key)
        {
            return GetStringAsync(Table, Key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Set the value as string by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<bool> SetStringAsync(this IKvTable Table, string Key, string Value, CancellationToken Token = default)
        {
            var Blob = Value != null
                ? Encoding.UTF8.GetBytes(Value)
                : null;

            return await Table.SetAsync(Key, Blob, Token);
        }

        /// <summary>
        /// Set the value as string by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool SetString(this IKvTable Table, string Key, string Value)
        {
            return SetStringAsync(Table, Key, Value).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get the value as <see cref="JObject"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<JObject> GetBsonObjectAsync(this IKvTable Table, string Key, CancellationToken Token = default)
        {
            var Blob = await Table.GetAsync(Key, Token);
            if (Blob is not null)
            {
                using var BsonReader = new BsonDataReader(new MemoryStream(Blob));
                return JsonSerializer.CreateDefault().Deserialize<JObject>(BsonReader);
            }

            return null;
        }

        /// <summary>
        /// Get the value as <see cref="JObject"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static JObject GetBsonObject(this IKvTable Table, string Key)
        {
            return Table.GetBsonObjectAsync(Key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Set the value as <see cref="JObject"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<bool> SetBsonObjectAsync(this IKvTable Table, string Key, JObject Value, CancellationToken Token = default)
        {
            if (Value is null)
                return await Table.SetAsync(Key, null, Token);

            using var Stream = new MemoryStream();
            using (var BsonWriter = new BsonDataWriter(Stream))
                JsonSerializer.CreateDefault().Serialize(BsonWriter, Value);

            return await Table.SetAsync(Key, Stream.ToArray(), Token);
        }

        /// <summary>
        /// Set the value as <see cref="JObject"/> by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool SetBsonObject(this IKvTable Table, string Key, JObject Value)
        {
            return Table.SetBsonObjectAsync(Key, Value).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get the value as entity with BSON encoding by its key.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static async Task<TEntity> GetBsonAsync<TEntity>(this IKvTable Table, string Key, CancellationToken Token = default)
        {
            var Bson = await Table.GetBsonObjectAsync(Key, Token);
            if (Bson is not null)
            {
                return Bson.ToObject<TEntity>();
            }

            return default;
        }

        /// <summary>
        /// Get the value as entity with BSON encoding by its key.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static TEntity GetBson<TEntity>(this IKvTable Table, string Key)
        {
            return Table.GetBsonAsync<TEntity>(Key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Set the value as entity with BSON encoding by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public static Task<bool> SetBsonAsync(this IKvTable Table, string Key, object Value, CancellationToken Token = default)
        {
            return Table.SetBsonObjectAsync(Key, Value is null ? null : JObject.FromObject(Value), Token);
        }

        /// <summary>
        /// Set the value as entity with BSON encoding by its key.
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool SetBson(this IKvTable Table, string Key, object Value)
        {
            return Table.SetBsonAsync(Key, Value).GetAwaiter().GetResult();
        }
    }
}
