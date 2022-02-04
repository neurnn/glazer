using Glazer.Blockchains.Models.Interfaces;
using Glazer.Core.Cryptography;
using Glazer.Core.Cryptography.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models
{
    public class CodeRef : IEncodable, IEquatable<CodeRef>, IEncodeToJObject, IDecodeFromJObject
    {
        private static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        /// None.
        /// </summary>
        private static readonly CodeRef X_NONE = new CodeRef();

        private Guid? m_CodeId;

        /// <summary>
        /// Initialize a new <see cref="CodeRef"/> instance.
        /// </summary>
        public CodeRef()
        {
            CodeKind = null;
            CodeBody = EMPTY_BYTES;
        }

        /// <summary>
        /// Initialize a new <see cref="CodeRef"/> instance.
        /// </summary>
        /// <param name="Controller"></param>
        /// <param name="Method"></param>
        public CodeRef(Type Controller, MethodInfo Method)
        {
            CodeKind = $"pb:{Controller.FullName}:{Method.Name}";
            CodeBody = EMPTY_BYTES;
        }

        /// <summary>
        /// Initialize a new <see cref="CodeRef"/> instance that points the Jint (ECMAScript 2019) script.
        /// </summary>
        /// <param name="Script"></param>
        /// <param name="Function"></param>
        public CodeRef(string Script, string Function)
        {
            CodeKind = $"jint:{Function}";
            CodeBody = Encoding.UTF8.GetBytes(Script);
        }

        /// <summary>
        /// Initialize a new <see cref="CodeRef"/> instance that refers the code repository.
        /// </summary>
        /// <param name="Guid"></param>
        /// <param name="Function"></param>
        public CodeRef(Guid Guid, string Function)
        {
            CodeKind = $"ref:{Guid}:{Function}";
            CodeBody = EMPTY_BYTES;
        }

        /// <summary>
        /// Initialize a new <see cref="CodeRef"/> instance that is custom code kind.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Kind"></param>
        /// <param name="Body"></param>
        public CodeRef(string Type, string Kind, byte[] Body)
        {
            CodeKind = $"{Type}:{Kind}";
            CodeBody = Body ?? EMPTY_BYTES;
        }

        /* Comparison operators */
        public static bool operator ==(CodeRef Left, CodeRef Right) =>  Left.Equals(Right);
        public static bool operator !=(CodeRef Left, CodeRef Right) => !Left.Equals(Right);

        /// <summary>
        /// Type of the code ref. (Not serialized)
        /// </summary>
        public CodeRefType RefType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CodeKind))
                    return CodeRefType.None;

                if (CodeKind.StartsWith("pb:"))
                    return CodeRefType.Prebuilt;

                if (CodeKind.StartsWith("ref:"))
                    return CodeRefType.Reference;

                if (CodeKind.StartsWith("jint:"))
                    return CodeRefType.JavaScript_Jint;

                return CodeRefType.Custom;
            }
        }

        private string m_CodeKind;
        private byte[] m_CodeBody;

        /// <summary>
        /// Code Target to invoke.<br />
        /// e.g. Prebuilt feature: "pb:controller:method" and the <see cref="CodeBody"/> should be empty. <br />
        /// e.g. Jint (ECMAScript 2019): "jint:object.function" and the <see cref="CodeBody"/> should have the script in binary, utf-8.<br />
        /// e.g. To refer the code table: "ref:[CODE GUID]:object.function" and the <see cref="CodeBody"/> should have the script in binary, utf-8.
        /// </summary>
        public string CodeKind
        {
            get => m_CodeKind;
            set
            {
                if (m_CodeKind != value)
                {
                    m_CodeKind = value;
                    m_CodeId = null;
                }
            }
        }

        /// <summary>
        /// Code Body to invoke. <br />
        /// e.g. Prebuilt feature: "pb:controller:method" and the <see cref="CodeBody"/> should be empty. <br />
        /// e.g. Jint (ECMAScript 2019): "jint:object.function" and the <see cref="CodeBody"/> should have the script in binary, utf-8.<br />
        /// e.g. To refer the code table: "ref:[CODE GUID]:object.function" and the <see cref="CodeBody"/> should have the script in binary, utf-8.
        /// </summary>
        public byte[] CodeBody
        {
            get => m_CodeBody;
            set
            {
                if (m_CodeBody != value)
                {
                    m_CodeBody = value;
                    m_CodeId = null;
                }
            }
        }

        /// <summary>
        /// Code Id. (generated by <see cref="MD5"/> algorithm)
        /// </summary>
        public Guid CodeId
        {
            get
            {
                if (!m_CodeId.HasValue)
                {
                    using var Stream = new MemoryStream();
                    using(var Writer = new EndianessWriter(Stream, null, true, true))
                    {
                        Encode(Writer, null);
                        Writer.Flush();
                    }

                    using (var Md5 = MD5.Create())
                    {
                        Stream.Position = 0;
                        m_CodeId = new Guid(Md5.ComputeHash(Stream));
                    }
                }

                return m_CodeId.Value;
            }
        }

        /// <summary>
        /// Try to get controller instance.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public bool TryGetController(Func<string, Type> Resolver, out Type Type, out MethodInfo Method)
        {
            if (!string.IsNullOrWhiteSpace(CodeKind) && RefType == CodeRefType.Prebuilt)
            {
                var Tokens = CodeKind.Split(':', 3);
                if (Tokens.Length == 3 && (Type = Resolver(Tokens[1])) != null)
                {
                    Method = Type.GetMethod(Tokens[2], BindingFlags.Instance | BindingFlags.Public) 
                          ?? Type.GetMethod(Tokens[2], BindingFlags.Instance | BindingFlags.NonPublic);

                    return Method != null;
                }
            }

            Type = null;
            Method = null;
            return false;
        }

        /// <summary>
        /// Try to get script body and its function.
        /// </summary>
        /// <param name="Script"></param>
        /// <param name="Function"></param>
        /// <returns></returns>
        public bool TryGetScript(out string Script, out string Function)
        {
            if (RefType == CodeRefType.JavaScript_Jint)
            {
                var Collon = CodeKind.IndexOf(':');
                if (Collon >= 0 && !string.IsNullOrWhiteSpace(Function = CodeKind.Substring(Collon + 1).Trim()))
                    return !string.IsNullOrWhiteSpace(Script = Encoding.UTF8.GetString(CodeBody));
            }

            Script = null;
            Function = null;
            return false;
        }

        /// <summary>
        /// Try to get code reference guid and its function name.
        /// </summary>
        /// <param name="CodeGuid"></param>
        /// <param name="Function"></param>
        /// <returns></returns>
        public bool TryGetReference(out Guid CodeGuid, out string Function)
        {
            if (RefType == CodeRefType.Reference)
            {
                var Tokens = CodeKind.Split(':', 3);
                if (Tokens.Length == 3 && Guid.TryParse(Tokens[1], out CodeGuid))
                    return !string.IsNullOrWhiteSpace(Function = Tokens.Last());
            }

            CodeGuid = default;
            Function = null;
            return false;
        }

        /// <summary>
        /// Encode the <see cref="CodeRef"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Options"></param>
        public void Encode(BinaryWriter Writer, NodeOptions Options)
        {
            Writer.Write(CodeKind ?? "");
            if (!string.IsNullOrWhiteSpace(CodeKind))
            {
                Writer.Write(CodeBody.Length);
                Writer.Write(CodeBody);
            }
        }

        /// <summary>
        /// Encode the code to <see cref="JObject"/>.
        /// </summary>
        /// <returns></returns>
        public JObject EncodeToJObject(NodeOptions Options)
        {
            var New = new JObject();

            New["kind"] = CodeKind;
            New["body"] = Base58.Encode(CodeBody ?? EMPTY_BYTES);

            return New;
        }

        /// <summary>
        /// Decode the <see cref="CodeRef"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Options"></param>
        public void Decode(BinaryReader Reader, NodeOptions Options)
        {
            if (!string.IsNullOrWhiteSpace(CodeKind = Reader.ReadString()))
                CodeBody = Reader.ReadBytes(Reader.ReadInt32()) ?? EMPTY_BYTES;

            else
                CodeBody = EMPTY_BYTES;
        }

        /// <summary>
        /// Decode the code from <see cref="JObject"/>.
        /// </summary>
        /// <returns></returns>
        public void DecodeFromJObject(JObject JObject, NodeOptions Options)
        {
            CodeKind = JObject.Value<string>("kind");
            CodeBody = Base58.Decode(JObject.Value<string>("body"));
        }

        /// <summary>
        /// Test whether the other code-ref is exactly same or not.
        /// </summary>
        /// <param name="Other"></param>
        /// <returns></returns>
        public bool Equals(CodeRef Other)
        {
            if (ReferenceEquals(this, Other ??= X_NONE))
                return true;

            if (CodeKind == Other.CodeKind &&
                CodeBody.SequenceEqual(Other.CodeBody))
                return true;

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(object Obj)
        {
            if (Obj is CodeRef Cr)
                return Equals(Cr);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(CodeKind ?? "", CodeBody ?? EMPTY_BYTES);

    }
}
