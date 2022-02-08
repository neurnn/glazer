using Backrole.Crypto;
using Glazer.Nodes.Exceptions;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Notations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Models.Interfaces
{
    public static class Messages
    {
        private static readonly object[] EMPTY_ARGS = new object[0];
        private static Dictionary<Guid, Type> m_N2T = new();
        private static Dictionary<Type, Guid> m_T2N = new();

        /// <summary>
        /// Map the `Glazer.Nodes` assembly types.
        /// </summary>
        static Messages() => Map(typeof(Messages).Assembly);

        /// <summary>
        /// Map an assembly to message encoder/decoder.
        /// </summary>
        /// <param name="Assembly"></param>
        public static void Map(Assembly Assembly) => Map(Assembly.GetTypes());

        /// <summary>
        /// Map types to message encoder/decoder.
        /// </summary>
        /// <param name="Types"></param>
        public static void Map(params Type[] Types)
        {
            var Mappings = CollectMappings(Types);
            m_N2T.Locked(N => m_T2N.Locked(T =>
            {
                while (Mappings.TryDequeue(out var Tuple))
                {
                    N[Tuple.Guid] = Tuple.Type;
                    T[Tuple.Type] = Tuple.Guid;
                }
            }));
        }

        /// <summary>
        /// Test whether the specified type has been mapped or not.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static bool IsMapped(Type Type) => m_T2N.Locked(X => X.ContainsKey(Type));

        /// <summary>
        /// Test whether the specified guid has been mapped or not.
        /// </summary>
        /// <param name="Guid"></param>
        /// <returns></returns>
        public static bool IsMapped(Guid Guid) => m_N2T.Locked(X => X.ContainsKey(Guid));

        /// <summary>
        /// Make <see cref="Guid"/> that points the <see cref="Type"/>.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static Guid MakeGuid(Type Type)
        {
            var Attribute = Type.GetCustomAttribute<NodeMessageAttribute>();
            var Name = (Attribute != null ? Attribute.Name : null) ?? Type.FullName;
            var Hash128 = Hashes.Default.Hash("MD5", Encoding.UTF8.GetBytes(Name));
            return new Guid(Hash128.Value);
        }

        /// <summary>
        /// Collect Mapping tuples from types.
        /// </summary>
        /// <param name="Types"></param>
        /// <returns></returns>
        private static Queue<(Guid Guid, Type Type)> CollectMappings(Type[] Types)
        {
            var Mappings = new Queue<(Guid Guid, Type Type)>();
            foreach (var Each in Types)
                Mappings.Enqueue((MakeGuid(Each), Each));

            return Mappings;
        }

        /// <summary>
        /// Try to get <see cref="Guid"/> of the mapped type.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Guid"></param>
        /// <returns></returns>
        public static bool TryGet(Type Type, out Guid Guid)
        {
            lock(m_T2N)
            {
                return m_T2N.TryGetValue(Type, out Guid);
            }
        }

        /// <summary>
        /// Gets <see cref="Guid"/> of the mapped type.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static Guid Get(Type Type)
        {
            if (!TryGet(Type, out var Guid))
                throw new IncompletedException("the type has not mapped.");

            return Guid;
        }

        /// <summary>
        /// Try to get <see cref="Type"/> of the mapped guid.
        /// </summary>
        /// <param name="Guid"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static bool TryGet(Guid Guid, out Type Type)
        {
            lock (m_N2T)
            {
                return m_N2T.TryGetValue(Guid, out Type);
            }
        }

        /// <summary>
        /// Gets <see cref="Type"/> of the mapped guid.
        /// </summary>
        /// <param name="Guid"></param>
        /// <returns></returns>
        public static Type Get(Guid Guid)
        {
            if (!TryGet(Guid, out var Type))
                throw new IncompletedException("the type has not mapped.");

            return Type;
        }

        /// <summary>
        /// Try Encode a message to the <see cref="JObject"/>.
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static bool TryEncode(JObject Output, object Input)
        {
            if (Input is not IJsonMessage Json)
                return false;

            if (!TryGet(Input.GetType(), out var Guid))
                return false;

            var Data = new JObject();
            Json.Encode(Data);

            Output["guid"] = Guid.ToString();
            Output["data"] = Data;

            return true;
        }

        /// <summary>
        /// Try Decode a message from the <see cref="JObject"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryDecode(JObject Input, out object Output)
        {
            var GuidStr = Input.Value<string>("guid");
            var Data = Input.Value<JObject>("data");

            if (Data != null && !string.IsNullOrWhiteSpace(GuidStr) && 
                System.Guid.TryParse(GuidStr, out var Guid))
            {
                if (TryGet(Guid, out var Type))
                {
                    Output = Data.ToObject(Type);
                    return true;
                }
            }

            Output = null;
            return false;
        }

        /// <summary>
        /// Try Encode a message to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static bool TryEncode(BinaryWriter Writer, object Input)
        {
            if (Input is not IBinaryMessage Binary)
                return false;

            if (!TryGet(Input.GetType(), out var Guid))
                return false;

            Writer.Write(Guid.ToByteArray());
            Binary.Encode(Writer);
            return true;
        }

        /// <summary>
        /// Try Decode a message from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryDecode(BinaryReader Reader, out object Output)
        {
            if (TryGet(new Guid(Reader.ReadBytes(16)), out var Type))
            {
                var Instance = Type.GetConstructor(Type.EmptyTypes).Invoke(EMPTY_ARGS);
                if (Instance is IBinaryMessage Binary)
                {
                    Binary.Decode(Reader);
                    Output = Binary;
                    return true;
                }
            }

            Output = null;
            return false;
        }

        /// <summary>
        /// Encode a message to the <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        public static void Encode(JObject Json, object Instance)
        {
            if (!TryEncode(Json, Instance))
                throw new PreconditionFailedException("No message mapped.");
        }

        /// <summary>
        /// Encode a message to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        public static void Encode(BinaryWriter Writer, object Instance)
        {
            if (!TryEncode(Writer, Instance))
                throw new PreconditionFailedException("No message mapped.");
        }

        /// <summary>
        /// Decode a message from the <see cref="JObject"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static object Decode(JObject Json)
        {
            if (!TryDecode(Json, out var Instance))
                throw new FormatException("No message instance available.");

            return Instance;
        }

        /// <summary>
        /// Decode a message from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static object Decode(BinaryReader Reader)
        {
            if (!TryDecode(Reader, out var Instance))
                throw new FormatException("No message instance available.");

            return Instance;
        }
    }
}
