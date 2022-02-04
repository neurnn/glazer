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
        private static readonly List<string> EMPTY_ACTORS = new List<string>();
        private static readonly IEnumerable<Seal> EMPTY_SEALS = new Seal[0];
        private static readonly CodeRef EMPTY_CODE = new CodeRef();


        /// <summary>
        /// Version of the transaction.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Flag values of the transaction.
        /// </summary>
        public uint Flags { get; set; } = 0;

        /// <summary>
        /// Time Stamp.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Code Reference to invoke.
        /// </summary>
        public CodeRef Code { get; set; }

        /// <summary>
        /// Verification Code Reference to invoke.
        /// The function must take the expected result and real result like: 
        /// <code>function verify(real_result, expected_result [, record_repository]) { ... }</code>
        /// </summary>
        public CodeRef VerifyCode { get; set; }

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
        /// Actors that refered by the transaction.
        /// </summary>
        public List<string> Actors { get; set; }

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
        public void Update(NodeOptions Options) => Hash = MakeHashValue(this, Options, false);

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
        public static HashValue MakeHashValue(Transaction Transaction, NodeOptions Options, bool Sealed = false)
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
        public VerificationStatus Verify(NodeOptions Options, bool Enforce = false)
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
        public void Encode(BinaryWriter Writer, NodeOptions Options)
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
        public void EncodeUnsealed(BinaryWriter Writer, NodeOptions Options)
        {
            var ParamBytes = Parameter.EncodeAsBson();
            var ResultBytes = ExpectedResult.EncodeAsBson();

            Writer.Write(Version);
            Writer.Write(Flags);
            Writer.Write(TimeStamp.ToSeconds(Options.Epoch));

            (Code ?? EMPTY_CODE).Encode(Writer, Options);
            (VerifyCode ?? EMPTY_CODE).Encode(Writer, Options);

            Writer.Write(ParamBytes.Length);
            Writer.Write(ResultBytes.Length);
            Writer.Write((Actors ?? EMPTY_ACTORS).Count);

            Writer.Write(ParamBytes);
            Writer.Write(ResultBytes);

            if (Actors != null)
            {
                foreach(var Actor in Actors)
                    Writer.Write(Actor ?? "");
            }
        }

        /// <summary>
        /// Decode the transaction from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        public void Decode(BinaryReader Reader, NodeOptions Options)
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
        public void DecodeUnsealed(BinaryReader Reader, NodeOptions Options)
        {
            Version = Reader.ReadUInt32();
            Flags = Reader.ReadUInt32();
            TimeStamp = Reader.ReadDouble().ToDateTime(Options.Epoch);

            (Code = new CodeRef()).Decode(Reader, Options);
            (VerifyCode = new CodeRef()).Decode(Reader, Options);

            var ParamLength = Reader.ReadInt32();
            var ResultLength = Reader.ReadInt32();
            var ActorLength = Reader.ReadInt32();

            Parameter = Reader.ReadBytes(ParamLength).DecodeAsBson();
            ExpectedResult = Reader.ReadBytes(ResultLength).DecodeAsBson();
            Actors = new List<string>();

            for (var i = 0; i < ActorLength; ++i)
                Actors.Add(Reader.ReadString());
        }

        /// <summary>
        /// Encode the transaction to <see cref="JObject"/>.
        /// </summary>
        /// <returns></returns>
        public JObject EncodeToJObject(NodeOptions Options)
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
            New["code"] = Code.EncodeToJObject(Options);
            New["code_verify"] = Code.EncodeToJObject(Options);
            New["parameter"] = Parameter;
            New["expects"] = ExpectedResult;
            New["actors"] = new JArray(Actors.ToArray());

            return New;
        }

        /// <summary>
        /// Decode the transaction from <see cref="JObject"/>.
        /// </summary>
        /// <param name="JObject"></param>
        public void DecodeFromJObject(JObject JObject, NodeOptions Options)
        {
            Hash = HashValue.Parse(JObject.Value<string>("hash") ?? "null");
            Version = JObject.Value<uint>("version");
            TimeStamp = JObject.Value<double>("timestamp").ToDateTime(Options.Epoch);
            (Code = new CodeRef()).DecodeFromJObject(JObject.Value<JObject>("code"), Options);
            (VerifyCode = new CodeRef()).DecodeFromJObject(JObject.Value<JObject>("code_verify"), Options);
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

            var Actors = JObject.Value<JArray>("actors");
            this.Actors = new List<string>();

            for(var i = 0; i < Actors.Count; ++i)
                this.Actors.Add(Actors[i].Value<string>());
        }
    }
}
