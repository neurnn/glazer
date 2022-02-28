using Backrole.Crypto;
using Glazer.Common.Common;
using Glazer.Kvdb.Abstractions;
using Glazer.Kvdb.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Common.Models
{
    public class BlockRequest
    {
        private List<Transaction> m_Transactions;
        private List<WitnessActor> m_Witness;
        private MemoryKvTable m_Data;
        private WitnessActor m_Producer;

        /// <summary>
        /// Time Stamp.
        /// </summary>
        public TimeStamp TimeStamp { get; set; } = TimeStamp.Now;

        /// <summary>
        /// Previous Block Reference.
        /// </summary>
        public BlockRef Previous { get; set; }

        /// <summary>
        /// Transactions.
        /// </summary>
        public List<Transaction> Transactions
        {
            get => ModelHelpers.OnDemand(ref m_Transactions);
            set => m_Transactions = value;
        }

        /// <summary>
        /// Witness Actors.
        /// </summary>
        public List<WitnessActor> Witness
        {
            get => ModelHelpers.OnDemand(ref m_Witness);
            set => m_Witness = value;
        }

        /// <summary>
        /// Key Value Table.
        /// </summary>
        public MemoryKvTable Data
        {
            get => ModelHelpers.OnDemand(ref m_Data);
            set => m_Data = value;
        }

        /// <summary>
        /// Producer.
        /// </summary>
        public WitnessActor Producer
        {
            get => m_Producer;
            set => m_Producer = value;
        }

        /// <summary>
        /// Import the <see cref="BlockRequest"/> from the reader.
        /// </summary>
        /// <param name="Reader"></param>
        public void Import(BinaryReader Reader)
        {
            if (Reader.Read7BitEncodedInt() > Block.CurrentVersion)
                throw new NotSupportedException("not supported version.");

            Transactions.Clear();
            Witness.Clear();

            Previous = Reader.ReadBlockRef();
            (Data = new MemoryKvTable()).Import(Reader);

            var Count = Reader.Read7BitEncodedInt();
            for (var i = 0; i < Count; ++i)
                Transactions.Add(Reader.ReadTransaction());

            Count = Reader.Read7BitEncodedInt();
            for(var i = 0; i < Count; ++i)
                Witness.Add(Reader.ReadWitnessActor());

            Producer = Reader.ReadWitnessActor();
        }

        /// <summary>
        /// Export the <see cref="BlockRequest"/> to the writer.
        /// </summary>
        /// <param name="Writer"></param>
        public void Export(BinaryWriter Writer)
        {
            Writer.Write7BitEncodedInt(Block.CurrentVersion);
            Writer.Write(Previous);
            Data.Export(Writer);

            Writer.Write7BitEncodedInt(Transactions.Count);
            foreach (var Each in Transactions)
                Writer.Write(Each);

            Writer.Write7BitEncodedInt(Witness.Count);
            foreach (var Each in Witness)
                Writer.Write(Each);

            Writer.Write(Producer);
        }

        /// <summary>
        /// Sign the block request as an witness.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="KeyPair"></param>
        /// <param name="Replace"></param>
        public void SignAsWitness(Actor Actor, SignKeyPair KeyPair, bool Replace = false)
        {
            if (m_Witness != null && m_Witness.FirstOrDefault(X => X.Actor == Actor).IsValid)
            {
                if (!Replace)
                    throw new InvalidOperationException("the block request has been signed.");

                m_Witness.RemoveAll(X => X.Actor == Actor);
            }

            var Witness = ModelHelpers.Swap(ref m_Witness, null);
            var Producer = ModelHelpers.Swap(ref m_Producer, default);

            using var Writer = new PacketWriter(); Export(Writer);
            var Signature = KeyPair.SignSeal(Writer.ToByteArray());
            Witness.Add(new WitnessActor(Actor, Signature));

            ModelHelpers.Swap(ref m_Witness, ref Witness);
            ModelHelpers.Swap(ref m_Producer, ref Producer);
        }

        /// <summary>
        /// Sign the block request as the producer. 
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="KeyPair"></param>
        /// <param name="Replace"></param>
        public void SignAsProducer(Actor Actor, SignKeyPair KeyPair, bool Replace = false)
        {
            if (Producer.IsValid && !Replace)
                throw new InvalidOperationException("the block request has been signed.");

            using var Writer = new PacketWriter(); Export(Writer);
            var Signature = KeyPair.SignSeal(Writer.ToByteArray());
            Producer = new WitnessActor(Actor, Signature);
        }

        /// <summary>
        /// Verify the block request using the written informations.
        /// </summary>
        /// <returns></returns>
        public bool Verify()
        {
            byte[] ProducerBytes, WitnessBytes;
            var Producer = ModelHelpers.Swap(ref m_Producer, default);

            using (var Writer = new PacketWriter())
            {
                Export(Writer);
                ProducerBytes = Writer.ToByteArray();
            }

            var Witness = ModelHelpers.Swap(ref m_Witness, null);
            using (var Writer = new PacketWriter())
            {
                Export(Writer);
                WitnessBytes = Writer.ToByteArray();
            }

            m_Witness = Witness;
            m_Producer = Producer;

            foreach(var Each in Witness)
            {
                if (!Each.Signature.Verify(WitnessBytes))
                    return false;
            }

            return Producer.Signature.Verify(ProducerBytes);
        }

        /// <summary>
        /// Harden the <see cref="BlockRequest"/> to <see cref="Block"/> value.
        /// </summary>
        /// <returns></returns>
        public Block Harden()
        {
            if (!Producer.IsValid)
                throw new InvalidOperationException("No block request signed by the producer.");

            if (!Verify())
                throw new InvalidOperationException("No block passed the verification.");

            return new Block(TimeStamp, Previous, Transactions.ToArray(), Data.ToReadOnly(), Witness.ToArray(), Producer);
        }
    }
}
