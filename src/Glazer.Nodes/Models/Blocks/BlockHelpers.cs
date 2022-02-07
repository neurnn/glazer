using Backrole.Crypto;
using Glazer.Nodes.Exceptions;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models.Transactions;
using Glazer.Nodes.Records;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Transactions;

namespace Glazer.Nodes.Models.Blocks
{
    public static class BlockHelpers
    {
        /// <summary>
        /// Write the <see cref="BlockIndex"/> to <see cref="BinaryWriter"/> with options.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Index"></param>
        public static void Write(this BinaryWriter Writer, BlockIndex Index)
        {
            Writer.Write(Index.H32);
            Writer.Write(Index.L32);
        }

        /// <summary>
        /// Read the <see cref="BlockIndex"/> from <see cref="BinaryWriter"/> with options.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static BlockIndex ReadBlockIndex(this BinaryReader Reader)
        {
            var H32 = Reader.ReadUInt32();
            var L32 = Reader.ReadUInt32();
            return new BlockIndex(H32, L32);
        }

        /// <summary>
        /// Write the <see cref="Block"/> to <see cref="BinaryWriter"/> with options.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Transaction"></param>
        public static void Write(this BinaryWriter Writer, Block Block, BlockPackingOptions Options)
        {
            var Header = Block.Header;
            var Witness = Block.Witness;
            var Transactions = Block.Transactions;

            if (Header.Index == BlockIndex.Zero)
                throw new IncompletedException("No block index set.");

            if (Header.PrevBlockIndex >= Header.Index)
                throw new IncompletedException("No previous block index set.");

            if (!Header.PrevBlockHash.IsValid)
                throw new IncompletedException("No previous block hash set.");

            // -- 
            if (Options.WithHash && !Header.Hash.IsValid)
                throw new IncompletedException("No block hash set.");

            if (Options.WithWitness)
            {
                if (Witness.Accounts.Count <= 0)
                    throw new IncompletedException("No witness added.");

                if (Witness.Accounts.Count != Witness.AccountSeals.Count)
                    throw new IncompletedException("No witness seals are matched.");
            }

            if (Options.WithProducer)
            {
                if (!Header.Producer.IsValid)
                    throw new IncompletedException("No producer set.");

                if (!Header.ProducerSign.IsValid)
                    throw new IncompletedException("No producer sign set.");
            }

            // -- Headers.
            Writer.Write(Header.Version);
            Writer.Write(Header.Index);
            Writer.Write(Header.TimeStamp);
            Writer.Write(Header.PrevBlockIndex);
            Writer.Write(Header.PrevBlockHash);

            Writer.Write(Options.WithHash ? Header.Hash : HashValue.Empty);
            Writer.Write(Options.WithProducer ? Header.Producer : Account.Empty);
            Writer.Write(Options.WithProducer ? Header.ProducerSign : SignValue.Empty);

            // -- Transactions. 
            Writer.Write7BitEncodedInt(Transactions.Count);
            foreach (var Each in Transactions) 
                Writer.Write(Each, TransactionPackingOptions.Full);

            // -- Witness
            if (!Options.WithWitness)
                Writer.Write7BitEncodedInt(0);

            else
            {
                Writer.Write7BitEncodedInt(Witness.Accounts.Count);
                for(var i = 0; i < Witness.Accounts.Count; ++i)
                {
                    Writer.Write(Witness.Accounts[i]);
                    Writer.Write(Witness.AccountSeals[i]);
                }
            }
        }

        /// <summary>
        /// Read the <see cref="Block"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Recyle"></param>
        /// <returns></returns>
        public static Block ReadBlock(this BinaryReader Reader, Block Recyle = null)
        {
            var Block = Recyle ?? new Block();
            var Header = Block.Header;
            var Witness = Block.Witness;
            var Transactions = Block.Transactions;

            Witness.Accounts.Clear();
            Witness.AccountSeals.Clear();
            Transactions.Clear();

            // -- Headers.
            Header.Version = Reader.ReadUInt32();
            Header.Index = Reader.ReadBlockIndex();
            Header.TimeStamp = Reader.ReadDateTime();
            Header.PrevBlockIndex = Reader.ReadBlockIndex();
            Header.PrevBlockHash = Reader.ReadHashValue();

            Header.Hash = Reader.ReadHashValue();
            Header.Producer = Reader.ReadAccount();
            Header.ProducerSign = Reader.ReadSignValue();

            // -- Transactions.
            var TrxLength = Reader.Read7BitEncodedInt();
            for (var i = 0; i < TrxLength; ++i)
                Transactions.Add(Reader.ReadTransaction());

            // -- Witness
            var WitnessLength = Reader.Read7BitEncodedInt();
            for (var i = 0; i < WitnessLength; ++i)
            {
                Witness.Accounts.Add(Reader.ReadAccount());
                Witness.AccountSeals.Add(Reader.ReadSignValue());
            }

            return Block;
        }

        /// <summary>
        /// Make the hash value for generating <see cref="HashValue"/> with options.
        /// </summary>
        /// <returns></returns>
        public static HashValue MakeHashValue(this Block Block, BlockPackingOptions Options = default)
        {
            using var Stream = new MemoryStream();
            using (var Writer = new BinaryWriter(Stream))
                Writer.Write(Block, Options);

            Stream.Position = 0;
            return Hashes.Default.Hash("SHA256", Stream);
        }

        /// <summary>
        /// Test the block hash preconditions.
        /// </summary>
        /// <param name="Header"></param>
        private static void TestBlockHashPreconditions(BlockHeader Header)
        {
            if (Header.Index == BlockIndex.Zero)
                throw new PreconditionFailedException("No block index set.");

            if (Header.PrevBlockIndex >= Header.Index)
                throw new PreconditionFailedException("No previous block index set.");

            if (!Header.PrevBlockHash.IsValid)
                throw new PreconditionFailedException("No previous block hash set.");
        }

        /// <summary>
        /// Test the block witness preconditions.
        /// </summary>
        /// <param name="Witness"></param>
        private static void TestBlockWithnessPreconditions(BlockWitness Witness)
        {
            if (Witness.Accounts.Count <= 0)
                throw new IncompletedException("No witness added.");

            if (Witness.Accounts.Count != Witness.AccountSeals.Count)
                throw new IncompletedException("No witness seals are matched.");
        }

        /// <summary>
        /// Make the hash value for generating <see cref="HashValue"/> for <see cref="BlockHeader.Hash"/>.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static HashValue MakeBlockHash(this Block Block)
        {
            TestBlockHashPreconditions(Block.Header);
            return Block.MakeHashValue(BlockPackingOptions.Hash);
        }

        /// <summary>
        /// Make the hash value for generating <see cref="HashValue"/> for <see cref="BlockWitness"/>.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static HashValue MakeWitnessHash(this Block Block)
        {
            var Header = Block.Header;
            TestBlockHashPreconditions(Header);
            
            if (!Header.Hash.IsValid)
                throw new PreconditionFailedException("No block hash set.");

            return Block.MakeHashValue(BlockPackingOptions.Witness);
        }

        /// <summary>
        /// Make the hash value for generating <see cref="HashValue"/> for <see cref="Block.ProducerSign"/>.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static HashValue MakeProducerHash(this Block Block)
        {
            var Header = Block.Header;
            TestBlockHashPreconditions(Header);

            if (!Header.Hash.IsValid)
                throw new PreconditionFailedException("No block hash set.");

            TestBlockWithnessPreconditions(Block.Witness);
            return Block.MakeHashValue(BlockPackingOptions.Producer);
        }

        /// <summary>
        /// Make the hash value for generating <see cref="HashValue"/> for <see cref="BlockHeader.PrevBlockHash"/>.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static HashValue MakeReferenceHash(this Block Block)
        {
            var Header = Block.Header;
            TestBlockHashPreconditions(Header);

            if (!Header.Hash.IsValid)
                throw new PreconditionFailedException("No block hash set.");

            TestBlockWithnessPreconditions(Block.Witness);
            if (!Header.Producer.IsValid)
                throw new IncompletedException("No producer set.");

            if (!Header.ProducerSign.IsValid)
                throw new IncompletedException("No producer sign set.");

            return Block.MakeHashValue(BlockPackingOptions.Reference);
        }

        /// <summary>
        /// Convert to <see cref="JObject"/> instance.
        /// </summary>
        /// <param name="Header"></param>
        /// <returns></returns>
        public static JObject ToJson(this BlockHeader Header)
        {
            var New = new JObject();

            New["version"] = Header.Version;
            New["index"] = Header.Index.ToString();
            New["timestamp"] = Header.TimeStamp.ToUnixSeconds();
            New["prev_block_index"] = Header.PrevBlockIndex.ToString();
            New["prev_block_hash"] = Header.PrevBlockHash.ToString();
            New["hash"] = Header.Hash.ToString();
            New["producer"] = Header.Producer.ToJson();
            New["producer_sign"] = Header.ProducerSign.ToString();

            return New;
        }

        /// <summary>
        /// Convert to <see cref="JObject"/> instance.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static JObject ToJson(this Block Block)
        {
            var New = new JObject();

            New["headers"] = Block.Header.ToJson();

            return New;
        }
    }
}
