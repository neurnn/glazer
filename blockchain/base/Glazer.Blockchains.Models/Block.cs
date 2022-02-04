using Glazer.Blockchains.Models.Interfaces;
using Glazer.Core.Cryptography;
using Glazer.Core.Cryptography.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Glazer.Blockchains.Models
{
    public sealed class Block : IEncodable, IEncodableUnsealed, IVerifiable, IEncodeToJObject, IDecodeFromJObject
    {
        private BlockHeader m_Header;
        
        /// <summary>
        /// Header of the block.
        /// </summary>
        public BlockHeader Header
        {
            get => m_Header;
            set
            {
                if ((m_Header = value) != null)
                     m_Header.Block = this;
            }
        }

        /// <summary>
        /// Transaction records.
        /// </summary>
        public List<Transaction> Transactions { get; set; }

        /// <summary>
        /// Seals that granted the block generation.
        /// </summary>
        public List<Seal> Seals { get; set; }

        /// <summary>
        /// Status of the block (runtime only, not serialized)
        /// </summary>
        public BlockStatus Status { get; private set; } = BlockStatus.Created;

        /// <summary>
        /// Event that broadcasts about the status changes.
        /// </summary>
        public event Action<Block, BlockStatus> StatusChanged;

        /// <summary>
        /// Set the block status and notify its change.
        /// </summary>
        /// <param name="Status"></param>
        public void SetStatus(BlockStatus Status)
        {
            if (this.Status != Status)
            {
                this.Status = Status;
                StatusChanged?.Invoke(this, Status);
            }
        }

        /// <summary>
        /// Make the <see cref="HashValue"/> of the block.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static HashValue MakeHashValue(Block Block, NodeOptions Options, bool Sealed = false)
        {
            using (var Stream = new MemoryStream())
            {
                using (var Writer = new EndianessWriter(Stream, Encoding.UTF8, true, true))
                {
                    if (Sealed)
                        Block.Encode(Writer, Options);

                    else
                        Block.EncodeUnsealed(Writer, Options);
                }

                Stream.Position = 0;
                return Sha256.Instance.Hash(Stream);
            }
        }

        /// <summary>
        /// Encode the block to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Options"></param>
        public void Encode(BinaryWriter Writer, NodeOptions Options)
        {
            if (m_Header is null)
                throw new InvalidOperationException("No block header assigned.");

            m_Header.Encode(Writer, Options);
            if (Seals is null)
                Writer.Write((ushort)0);

            else
            {
                Writer.Write((ushort)Seals.Count);
                foreach (var Each in Seals)
                    Writer.Write(Each.ToString());
            }

            if (Transactions is null)
                Writer.Write((ushort)0);

            else
            {
                Writer.Write((ushort)Transactions.Count);
                foreach (var Each in Transactions)
                    Each.Encode(Writer, Options);
            }

        }

        /// <summary>
        /// Encode the block to the <see cref="BinaryWriter"/> without seal informations.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Options"></param>
        public void EncodeUnsealed(BinaryWriter Writer, NodeOptions Options)
        {
            if (m_Header is null)
                throw new InvalidOperationException("No block header assigned.");

            m_Header.EncodeUnsealed(Writer, Options);

            if (Transactions is null)
                Writer.Write((ushort) 0);

            else
            {
                Writer.Write((ushort)Transactions.Count);
                foreach (var Each in Transactions)
                    Each.EncodeUnsealed(Writer, Options);
            }
        }

        /// <summary>
        /// Decode the block from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Options"></param>
        public void Decode(BinaryReader Reader, NodeOptions Options)
        {
            if (m_Header is null)
                m_Header = new BlockHeader();

            m_Header.Decode(Reader, Options);
            ushort SealLength = Reader.ReadUInt16();
            Seals = new List<Seal>();
            for (var i = 0; i < SealLength; ++i)
                Seals.Add(Seal.Parse(Reader.ReadString()));

            ushort Count = Reader.ReadUInt16();
            Transactions = new List<Transaction>();
            for (var i = 0; i < Count; ++i)
            {
                var Trx = new Transaction();
                Trx.Decode(Reader, Options);
                Transactions.Add(Trx);
            }
        }

        /// <summary>
        /// Decode the block from the <see cref="BinaryReader"/> without seal informations.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Options"></param>
        public void DecodeUnsealed(BinaryReader Reader, NodeOptions Options)
        {
            if (m_Header is null)
                m_Header = new BlockHeader();

            m_Header.DecodeUnsealed(Reader, Options);

            ushort Count = Reader.ReadUInt16();
            Transactions = new List<Transaction>();
            for(var i = 0; i < Count; ++i)
            {
                var Trx = new Transaction();
                Trx.DecodeUnsealed(Reader, Options);
                Transactions.Add(Trx);
            }
        }

        /// <summary>
        /// Verify the block.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="Enforce"></param>
        /// <returns></returns>
        public VerificationStatus Verify(NodeOptions Options, bool Enforce = false)
        {
            if ((!Enforce && (Seals is null || Seals.Count <= 0)) || m_Header is null)
                return VerificationStatus.Incompleted;

            var Status = m_Header.Verify(Options, Enforce);
            if (Status == VerificationStatus.Okay && (Seals != null && Seals.Count > 0))
            {
                var HashValue = MakeHashValue(this, Options, false);
                foreach(var Seal in Seals)
                {
                    if (!Secp256k1.Instance.Verify(Seal.Signature, Seal.PublicKey, HashValue))
                        return VerificationStatus.SignatureError;
                }
            }

            return Status;
        }

        /// <summary>
        /// Encode the block to <see cref="JObject"/>.
        /// </summary>
        /// <returns></returns>
        public JObject EncodeToJObject(NodeOptions Options)
        {
            if (m_Header is null)
                throw new InvalidOperationException("No block header assigned.");

            var New = m_Header.EncodeToJObject(Options);
            var Trx = new JArray();

            if (Transactions != null)
            {
                foreach (var Each in Transactions)
                    Trx.Add(Each.EncodeToJObject(Options));
            }

            var Seal = new JArray();
            if (Seals != null)
            {
                foreach (var Each in Seals)
                    Seal.Add(Each.ToString());
            }

            New["seals"] = Seal;
            New["transactions"] = Trx;

            return New;
        }

        /// <summary>
        /// Decode the block from <see cref="JObject"/>.
        /// </summary>
        /// <param name="JObject"></param>
        public void DecodeFromJObject(JObject JObject, NodeOptions Options)
        {
            if (m_Header is null)
                m_Header = new BlockHeader();

            m_Header.DecodeFromJObject(JObject, Options);
            var SealArray = JObject.Value<JArray>("seals");
            Seals = new List<Seal>();

            for (var i = 0; i < SealArray.Count; ++i)
                Seals.Add(Seal.Parse(SealArray[i].Value<string>()));

            var Trxs = JObject.Value<JArray>("transactions");
            Transactions = new List<Transaction>();

            for(var i = 0; i < Trxs.Count; ++i)
            {
                var Trx = Trxs[i].ToObject<JObject>();
                var New = new Transaction();

                New.DecodeFromJObject(Trx, Options);
                Transactions.Add(New);
            }
        }
    }
}
