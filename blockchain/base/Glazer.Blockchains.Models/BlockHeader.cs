using Glazer.Blockchains.Models.Interfaces;
using Glazer.Blockchains.Models.Internals;
using Glazer.Core.Cryptography;
using Glazer.Core.Cryptography.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Glazer.Blockchains.Models
{
    public sealed class BlockHeader : IEncodable, IEncodableUnsealed, IVerifiable, IEncodeToJObject, IDecodeFromJObject
    {
        /// <summary>
        /// Block instance that is owner of the transaction.
        /// Sometimes, this value will be null.
        /// </summary>
        public Block Block { get; set; }

        /// <summary>
        /// Guid of the block.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Version of the block.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Previous Block's Guid.
        /// </summary>
        public Guid PrevGuid { get; set; }

        /// <summary>
        /// Previous Block's Hash.
        /// </summary>
        public HashValue PrevHash { get; set; }

        /// <summary>
        /// Time Stamp of the block.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Hash value of the block data.
        /// </summary>
        public HashValue Hash { get; set; }

        /// <summary>
        /// Update Hash information.
        /// </summary>
        /// <param name="Options"></param>
        public void Update(BlockOptions Options) => Hash = MakeHashValue(this, Options, false);

        /// <summary>
        /// Encode the block header to the <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="Writer"></param>
        public void Encode(BinaryWriter Writer, BlockOptions Options)
        {
            EncodeUnsealed(Writer, Options);
            Writer.Write(Hash.ToString());
        }

        /// <summary>
        /// Encode the block header to the <see cref="BinaryWriter"/> without hashs.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Options"></param>
        public void EncodeUnsealed(BinaryWriter Writer, BlockOptions Options)
        {
            Writer.Write(Guid.ToByteArray());
            Writer.Write(Version);
            Writer.Write(PrevGuid.ToByteArray());
            Writer.Write(PrevHash.ToString());
            Writer.Write(TimeStamp.ToSeconds(Options.Epoch));
        }

        /// <summary>
        /// Decode the block header from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Options"></param>
        public void Decode(BinaryReader Reader, BlockOptions Options)
        {
            DecodeUnsealed(Reader, Options);
            Hash = HashValue.Parse(Reader.ReadString());
        }

        /// <summary>
        /// Decode the block header from the <see cref="BinaryReader"/> without hashs.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Options"></param>
        public void DecodeUnsealed(BinaryReader Reader, BlockOptions Options)
        {
            Guid = new Guid(Reader.ReadBytes(16));
            Version = Reader.ReadUInt32();
            PrevGuid = new Guid(Reader.ReadBytes(16));
            PrevHash = HashValue.Parse(Reader.ReadString());
            TimeStamp = Reader.ReadDouble().ToDateTime(Options.Epoch);
        }

        /// <summary>
        /// Make the <see cref="HashValue"/> of the block header.
        /// </summary>
        /// <param name="Header"></param>
        /// <returns></returns>
        public static HashValue MakeHashValue(BlockHeader Header, BlockOptions Options, bool Sealed = false)
        {
            using (var Stream = new MemoryStream())
            {
                using (var Writer = new EndianessWriter(Stream, Encoding.UTF8, true, true))
                {
                    if (Sealed)
                        Header.Encode(Writer, Options);

                    else
                        Header.EncodeUnsealed(Writer, Options);

                    if (Header.Block != null && Header.Block.Transactions != null)
                    {
                        foreach (var Trx in Header.Block.Transactions)
                            Writer.Write(Transaction.MakeHashValue(Trx, Options, Sealed).Value);
                    }
                }

                Stream.Position = 0;
                return Sha256.Instance.Hash(Stream);
            }
        }

        /// <summary>
        /// Verify the <see cref="BlockHeader"/>.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="Enforce"></param>
        /// <returns></returns>
        public VerificationStatus Verify(BlockOptions Options, bool Enforce = false)
        {
            if (!Enforce && Hash == HashValue.Empty)
                return VerificationStatus.Incompleted;

            var NewHash = MakeHashValue(this, Options, false);
            if (NewHash != Hash)
                return VerificationStatus.HashError;

            return VerificationStatus.Okay;
        }

        /// <summary>
        /// Encode the block header to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public JObject EncodeToJObject(BlockOptions Options)
        {
            var New = new JObject();

            New["guid"] = Guid.ToString();
            New["hash"] = Hash.ToString();
            New["version"] = Version;
            New["prev.guid"] = PrevGuid.ToString();
            New["prev.hash"] = PrevHash.ToString();
            New["timestamp"] = TimeStamp.ToSeconds(Options.Epoch);

            return New;
        }

        /// <summary>
        /// Decode the block header from <see cref="JObject"/>.
        /// </summary>
        /// <param name="JObject"></param>
        /// <param name="Options"></param>
        public void DecodeFromJObject(JObject JObject, BlockOptions Options)
        {
            Guid = new Guid(JObject.Value<string>("guid"));
            Hash = HashValue.Parse(JObject.Value<string>("hash"));
            Version = JObject.Value<uint>("version");
            PrevGuid = new Guid(JObject.Value<string>("prev.guid"));
            PrevHash = HashValue.Parse(JObject.Value<string>("prev.hash"));
            TimeStamp = JObject.Value<double>("timestamp").ToDateTime(Options.Epoch);
        }
    }
}
