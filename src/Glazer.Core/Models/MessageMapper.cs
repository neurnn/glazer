using Backrole.Core.Abstractions;
using Backrole.Crypto;
using Glazer.Core.Exceptions;
using Glazer.Core.Helpers;
using Glazer.Core.Models.Interfaces;
using Glazer.Core.Notations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Glazer.Core.Models
{
    public class MessageMapper
    {
        private static readonly object[] EMPTY_ARGS = new object[0];
        private Dictionary<Guid, Type> m_N2T = new();
        private Dictionary<Type, Guid> m_T2N = new();

        /// <summary>
        /// Map the `Glazer.Nodes` assembly types.
        /// </summary>
        public MessageMapper() => Map(typeof(MessageMapper).Assembly);

        /// <summary>
        /// Map an assembly to message encoder/decoder.
        /// </summary>
        /// <param name="Assembly"></param>
        public MessageMapper Map(Assembly Assembly) => Map(Assembly.GetTypes());

        /// <summary>
        /// Map types to message encoder/decoder.
        /// </summary>
        /// <param name="Types"></param>
        public MessageMapper Map(params Type[] Types)
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

            return this;
        }

        /// <summary>
        /// Test whether the specified type has been mapped or not.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public bool IsMapped(Type Type) => m_T2N.Locked(X => X.ContainsKey(Type));

        /// <summary>
        /// Test whether the specified guid has been mapped or not.
        /// </summary>
        /// <param name="Guid"></param>
        /// <returns></returns>
        public bool IsMapped(Guid Guid) => m_N2T.Locked(X => X.ContainsKey(Guid));

        /// <summary>
        /// Make <see cref="Guid"/> that points the <see cref="Type"/>.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public Guid MakeGuid(Type Type)
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
        private Queue<(Guid Guid, Type Type)> CollectMappings(Type[] Types)
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
        public bool TryGet(Type Type, out Guid Guid)
        {
            lock (m_T2N)
            {
                return m_T2N.TryGetValue(Type, out Guid);
            }
        }

        /// <summary>
        /// Gets <see cref="Guid"/> of the mapped type.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public Guid Get(Type Type)
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
        public bool TryGet(Guid Guid, out Type Type)
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
        public Type Get(Guid Guid)
        {
            if (!TryGet(Guid, out var Type))
                throw new IncompletedException("the type has not mapped.");

            return Type;
        }

        /// <summary>
        /// Try Encode a message to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Input"></param>
        /// <returns></returns>
        public bool TryEncode(BinaryWriter Writer, object Input)
        {
            if (Input is not IMessage Binary)
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
        public bool TryDecode(BinaryReader Reader, out object Output, IServiceInjector Injector = null)
        {
            if (TryGet(new Guid(Reader.ReadBytes(16)), out var Type))
            {
                var Instance = Injector is null
                    ? Type.GetConstructor(Type.EmptyTypes).Invoke(EMPTY_ARGS)
                    : Injector.Create(Type);

                if (Instance is IMessage Binary)
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
        /// Encode a message to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        public void Encode(BinaryWriter Writer, object Instance)
        {
            if (!TryEncode(Writer, Instance))
                throw new PreconditionFailedException("No message mapped.");
        }

        /// <summary>
        /// Decode a message from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public object Decode(BinaryReader Reader, IServiceInjector Injector = null)
        {
            if (!TryDecode(Reader, out var Instance, Injector))
                throw new FormatException("No message instance available.");

            return Instance;
        }
    }
}
