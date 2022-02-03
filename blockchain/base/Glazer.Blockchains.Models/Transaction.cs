using Glazer.Blockchains.Models.Interfaces;
using Glazer.Blockchains.Models.Internals;
using Glazer.Core.Cryptography;
using Glazer.Core.Cryptography.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Glazer.Blockchains.Models
{
    public class Transaction : IEncodable, IEncodableUnsealed, IVerifiable, IEncodeToJObject, IDecodeFromJObject
    {
        private static readonly byte[] EMPTY_BYTES = new byte[0];
        private static readonly IEnumerable<Seal> EMPTY_SEALS = new Seal[0];

        /// <summary>
        /// Version of the transaction.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Time Stamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Blob that is target of the transaction.
        /// </summary>
        public Guid BlobId { get; set; }

        /// <summary>
        /// Latest ETag of the target blob.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Code Target to invoke.<br />
        /// e.g. Prebuilt feature: "pb:controller.method" and the <see cref="CodeBody"/> should be empty. <br />
        /// e.g. Jint (ECMAScript 2019): "jint:object.function" and the <see cref="CodeBody"/> should have the script in binary, utf-8.<br />
        /// </summary>
        public string CodeKind { get; set; }

        /// <summary>
        /// Code Body to invoke. <br />
        /// e.g. Prebuilt feature: "pb:controller.method" and the <see cref="CodeBody"/> should be empty. <br />
        /// e.g. Jint (ECMAScript 2019): "jint:object.function" and the <see cref="CodeBody"/> should have the script in binary, utf-8.<br />
        /// </summary>
        public byte[] CodeBody { get; set; }

        /// <summary>
        /// Argument object that is to be supplied to the code.
        /// </summary>
        public JObject Parameter { get; set; }

        /// <summary>
        /// Expected result that is calculated by the sender.
        /// Remote peers will test the result is same before putting the transaction to their node.
        /// </summary>
        public JObject ExpectedResult { get; set; }

        /// <summary>
        /// Hash of the transaction.
        /// And this will be used as TX id.
        /// (This will be generated without seals)
        /// </summary>
        public HashValue Hash { get; set; }

        /// <summary>
        /// Seals that is generated from remote peers.
        /// </summary>
        public List<Seal> Seals { get; set; }

        /// <summary>
        /// Status of the transaction (runtime only, not serialized)
        /// </summary>
        public TransactionStatus Status { get; private set; } = TransactionStatus.Created;

        /// <summary>
        /// Event that broadcasts about the status changes.
        /// </summary>
        public event Action<Transaction, TransactionStatus> StatusChanged;

        /// <summary>
        /// Update Hash information.
        /// </summary>
        /// <param name="Options"></param>
        public void Update(BlockOptions Options) => Hash = MakeHashValue(this, Options, false);

        /// <summary>
        /// Set the transaction status and notify its change.
        /// </summary>
        /// <param name="Status"></param>
        public void SetStatus(TransactionStatus Status)
        {
            if (this.Status != Status)
            {
                this.Status = Status;
                StatusChanged?.Invoke(this, Status);
            }
        }

        /// <summary>
        /// Make the <see cref="HashValue"/> of the transaction.
        /// </summary>
        /// <param name="Transaction"></param>
        /// <returns></returns>
        public static HashValue MakeHashValue(Transaction Transaction, BlockOptions Options, bool Sealed = false)
        {
            using (var Stream = new MemoryStream())
            {
                using (var Writer = new EndianessWriter(Stream, Encoding.UTF8, true, true))
                {
                    if (Sealed)
                        Transaction.Encode(Writer, Options);

                    else
                        Transaction.EncodeUnsealed(Writer, Options);
                }

                Stream.Position = 0;
                return Sha256.Instance.Hash(Stream);
            }
        }

        /// <summary>
        /// Verify the transaction.
        /// </summary>
        /// <returns></returns>
        public VerificationStatus Verify(BlockOptions Options, bool Enforce = false)
        {
            if (!Enforce && (Hash == HashValue.Empty || Seals.Count <= 1))
                return VerificationStatus.Incompleted;

            var NewHash = MakeHashValue(this, Options, false);
            if (NewHash != Hash)
                return VerificationStatus.HashError;

            foreach (var Each in Seals)
            {
                if (Secp256k1.Instance.Verify(Each.Signature, Each.PublicKey, Hash))
                    continue;

                return VerificationStatus.SignatureError;
            }

            return VerificationStatus.Okay;
        }

        /// <summary>
        /// Encode the transaction to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        public void Encode(BinaryWriter Writer, BlockOptions Options)
        {
            EncodeUnsealed(Writer, Options);

            Writer.Write(Hash.ToString());
            Writer.Write(Seals != null ? Seals.Count : 0);

            foreach(var Each in Seals ?? EMPTY_SEALS)
                Writer.Write(Each.ToString());
        }

        /// <summary>
        /// Encode the transaction to the <see cref="BinaryWriter"/> without seals.
        /// </summary>
        /// <param name="Writer"></param>
        public void EncodeUnsealed(BinaryWriter Writer, BlockOptions Options)
        {
            var ParamBytes = Parameter.EncodeAsBson();
            var ResultBytes = ExpectedResult.EncodeAsBson();
            var CodeBody = this.CodeBody ?? EMPTY_BYTES;

            Writer.Write(Version);
            Writer.Write(TimeStamp.ToSeconds(Options.Epoch));

            Writer.Write(BlobId.ToByteArray());
            Writer.Write(ETag ?? "");
            Writer.Write(CodeKind ?? "");

            Writer.Write(CodeBody.Length);
            Writer.Write(ParamBytes.Length);
            Writer.Write(ResultBytes.Length);

            Writer.Write(CodeBody);
            Writer.Write(ParamBytes);
            Writer.Write(ResultBytes);
        }

        /// <summary>
        /// Decode the transaction from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        public void Decode(BinaryReader Reader, BlockOptions Options)
        {
            DecodeUnsealed(Reader, Options);

            Hash = HashValue.Parse(Reader.ReadString());
            var Count = Reader.ReadInt32();

            Seals = new();
            for (var i = 0; i < Count; ++i)
                Seals.Add(Seal.Parse(Reader.ReadString()));
        }

        /// <summary>
        /// Decode the transaction from the <see cref="BinaryReader"/> without seals.
        /// </summary>
        /// <param name="Writer"></param>
        public void DecodeUnsealed(BinaryReader Reader, BlockOptions Options)
        {
            Version = Reader.ReadUInt32();
            TimeStamp = Reader.ReadDouble().ToDateTime(Options.Epoch);

            BlobId = new Guid(Reader.ReadBytes(16));
            ETag = Reader.ReadString();
            CodeKind = Reader.ReadString();

            var CodeLength = Reader.ReadInt32();
            var ParamLength = Reader.ReadInt32();
            var ResultLength = Reader.ReadInt32();

            CodeBody = Reader.ReadBytes(CodeLength) ?? EMPTY_BYTES;
            Parameter = Reader.ReadBytes(ParamLength).DecodeAsBson();
            ExpectedResult = Reader.ReadBytes(ResultLength).DecodeAsBson();
        }

        /// <summary>
        /// Encode the transaction to <see cref="JObject"/>.
        /// </summary>
        /// <returns></returns>
        public JObject EncodeToJObject(BlockOptions Options)
        {
            var New = new JObject();
            var Signs = new JArray();
            foreach (var Each in Seals)
            {
                var Seal = new JObject();

                Seal["key"] = Each.PublicKey.ToString();
                Seal["value"] = Each.Signature.ToString();

                Signs.Add(Seal);
            }

            New["hash"] = Hash.ToString();
            New["version"] = Version;
            New["timestamp"] = TimeStamp.ToSeconds(Options.Epoch);
            New["seals"] = Signs;
            New["blob.id"] = BlobId.ToString();
            New["blob.etag"] = ETag ?? "";
            New["code.kind"] = CodeKind;
            New["code.body"] = Base58.Encode(CodeBody ?? EMPTY_BYTES);
            New["parameter"] = Parameter;
            New["expects"] = ExpectedResult;

            return New;
        }

        /// <summary>
        /// Decode the transaction from <see cref="JObject"/>.
        /// </summary>
        /// <param name="JObject"></param>
        public void DecodeFromJObject(JObject JObject, BlockOptions Options)
        {
            Hash = HashValue.Parse(JObject.Value<string>("hash") ?? "null");
            Version = JObject.Value<uint>("version");
            TimeStamp = JObject.Value<double>("timestamp").ToDateTime(Options.Epoch);
            BlobId = new Guid(JObject.Value<string>("blob.id"));
            ETag = JObject.Value<string>("blob.etag");
            CodeKind = JObject.Value<string>("code.kind");
            CodeBody = Base58.Decode(JObject.Value<string>("code.body"));
            Parameter = JObject.Value<JObject>("parameter");
            ExpectedResult = JObject.Value<JObject>("expects");

            var Seals = JObject.Value<JArray>("seals");
            this.Seals = new List<Seal>();

            for(var i = 0; i < Seals.Count; ++i)
            {
                var Key = PublicKey.Parse(Seals[i].Value<string>("key"));
                var Value = SignatureValue.Parse(Seals[i].Value<string>("value"));
                this.Seals.Add(new Seal(Key, Value));
            }
        }
    }
}
