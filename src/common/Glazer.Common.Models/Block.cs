using Backrole.Crypto;
using Glazer.Common.Common;
using Glazer.Kvdb.Extensions;
using Glazer.Kvdb.Memory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Glazer.Common.Models
{
    /// <summary>
    /// Irresible Block information.
    /// </summary>
    public struct Block : IEquatable<Block>
    {
        /// <summary>
        /// Version information.
        /// </summary>
        public const int CurrentVersion = 0;

        /// <summary>
        /// Initialize a new <see cref="Block"/> instance.
        /// </summary>
        /// <param name="TimeStamp"></param>
        /// <param name="Previous"></param>
        /// <param name="Transactions"></param>
        /// <param name="Data"></param>
        /// <param name="Witness"></param>
        /// <param name="Producer"></param>
        internal Block(
            TimeStamp TimeStamp, BlockRef Previous, Transaction[] Transactions,
            MemoryKvReadOnlyTable Data, WitnessActor[] Witness, WitnessActor Producer)
        {
            this.TimeStamp = TimeStamp;
            this.Previous = Previous;
            this.Transactions = Transactions;
            this.Data = Data;
            this.Witness = Witness;
            this.Producer = Producer;
        }

        /* Comparison operators. */
        public static bool operator ==(Block L, Block R) => L.Equals(R);
        public static bool operator !=(Block L, Block R) => !L.Equals(R);

        /// <summary>
        /// Try to import the block from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static bool TryImport(BinaryReader Reader, out Block Block)
        {
            try
            {
                if (Reader.Read7BitEncodedInt() > CurrentVersion)
                {
                    Block = default;
                    return false;
                }

                var TimeStamp = Reader.ReadTimeStamp();
                var Previous = Reader.ReadBlockRef();

                var Transactions = new Transaction[Reader.Read7BitEncodedInt()];
                for (var i = 0; i < Transactions.Length; ++i)
                    Transactions[i] = Reader.ReadTransaction();

                var Data = new MemoryKvTable();
                Data.Import(Reader);

                var Witness = new WitnessActor[Reader.Read7BitEncodedInt()];
                for (var i = 0; i < Witness.Length; ++i)
                    Witness[i] = Reader.ReadWitnessActor();

                var Producer = Reader.ReadWitnessActor();
                Block = new Block(TimeStamp, Previous, Transactions, Data.ToReadOnly(), Witness, Producer);
                return true;
            }
            catch { }

            Block = default;
            return false;
        }

        /// <summary>
        /// Try to export the block into binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static bool TryExport(BinaryWriter Writer, Block Block)
        {
            try
            {
                var Transactions = Block.Transactions ?? Transaction.Empty;
                var Witness = Block.Witness ?? WitnessActor.Empty;

                Writer.Write7BitEncodedInt(CurrentVersion);
                Writer.Write(Block.TimeStamp);

                Writer.Write(Block.Previous);

                Writer.Write7BitEncodedInt(Transactions.Length);
                foreach (var Each in Transactions) Writer.Write(Each);

                Block.Data.Export(Writer);

                Writer.Write7BitEncodedInt(Witness.Length);
                foreach (var Each in Witness) Writer.Write(Each);

                Writer.Write(Block.Producer);
                return true;
            }

            catch { }
            return false;
        }

        /// <summary>
        /// Try to e
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static bool TryExport(JObject Json, Block Block)
        {
            try
            {
                Json["timestamp"] = Block.TimeStamp.Value;
                Json["previous"] = ModelHelpers.Export(BlockRef.TryExport, Block.Previous);
                Json["transactions"] = Block.Transactions.Export(Transaction.TryExport);

                var KV = new JObject();
                Block.Data.Export(KV);

                Json["data"] = KV;
                Json["witness"] = Block.Witness.Export(WitnessActor.TryExport);
                Json["producer"] = ModelHelpers.Export(WitnessActor.TryExport, Block.Producer);
                return true;
            }

            catch { }
            return false;
        }

        /// <summary>
        /// Time Stamp.
        /// </summary>
        public TimeStamp TimeStamp { get; }

        /// <summary>
        /// Previous Block Reference.
        /// </summary>
        public BlockRef Previous { get; }

        /// <summary>
        /// Transactions.
        /// </summary>
        public Transaction[] Transactions { get; }

        /// <summary>
        /// Key Value Table.
        /// </summary>
        public MemoryKvReadOnlyTable Data { get; }

        /// <summary>
        /// Witness Actors.
        /// </summary>
        public WitnessActor[] Witness { get; }

        /// <summary>
        /// Producer.
        /// </summary>
        public WitnessActor Producer { get; }

        /// <summary>
        /// Determines whether the transaction is valid or not.
        /// </summary>
        public bool IsValid => Previous.IsValid && Transactions != null && Witness != null && Producer.IsValid;

        /// <summary>
        /// Test whether the block contains the given transaction or not.
        /// </summary>
        /// <param name="Transaction"></param>
        /// <returns></returns>
        public bool Contains(Transaction Transaction)
        {
            return Transactions.FirstOrDefault(X => X.Id == Transaction.Id).IsValid;
        }

        /// <inheritdoc/>
        public bool Equals(Block Other)
        {
            if (TimeStamp != Other.TimeStamp)
                return false;

            if (Previous != Other.Previous)
                return false;

            if (!Transactions.SequenceEqualNullSafe(Other.Transactions))
                return false;

            foreach(var Each in Data)
            {
                var Value = Other.Data.Get(Each.Key);
                if (Value != Each.Value)
                {
                    if (Value is null || Each.Value is null)
                        return false;

                    if (!Value.SequenceEqual(Each.Value))
                        return false;
                }
            }

            if (!Witness.SequenceEqualNullSafe(Other.Witness))
                return false;

            if (Producer != Other.Producer)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is Block Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(
            TimeStamp, Previous, Transactions ?? Transaction.Empty, Data,
            Witness ?? WitnessActor.Empty, Producer);

        /// <summary>
        /// Verify the block using the written informations.
        /// </summary>
        /// <returns></returns>
        public bool Verify()
        {
            var Temp = new BlockRequest
            {
                TimeStamp = TimeStamp,
                Previous = Previous,
                Transactions = new List<Transaction>(Transactions),
                Witness = new List<WitnessActor>(Witness),
                Producer = Producer
            };

            Data.CopyTo(Temp.Data);
            return Temp.Verify();
        }
    }
}
