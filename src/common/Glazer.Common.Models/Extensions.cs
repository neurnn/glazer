using Backrole.Crypto;
using System;
using System.IO;

namespace Glazer.Common.Models
{
    public static class Extensions
    {
        /// <summary>
        /// Write the <see cref="Transaction"/> to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Transaction"></param>
        public static void Write(this BinaryWriter Writer, Transaction Transaction)
        {
            if (!Transaction.TryExport(Writer, Transaction))
                throw new ArgumentException("the transaction is not valid.");
        }

        /// <summary>
        /// Read the <see cref="Transaction"/> from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static Transaction ReadTransaction(this BinaryReader Reader)
        {
            if (!Transaction.TryImport(Reader, out var Result))
                throw new InvalidOperationException("the input binary contains invalid transaction.");

            return Result;
        }

        /// <summary>
        /// Write the <see cref="BlockRef"/> to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="BlockRef"></param>
        public static void Write(this BinaryWriter Writer, BlockRef BlockRef)
        {
            Writer.Write(BlockRef.Id.Guid);
            Writer.Write(BlockRef.Hash);
        }

        /// <summary>
        /// Read the <see cref="BlockRef"/> from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static BlockRef ReadBlockRef(this BinaryReader Reader)
        {
            var Id = new BlockId(Reader.ReadGuid());
            var Hash = Reader.ReadHashValue();
            return new BlockRef(Id, Hash);
        }

        /// <summary>
        /// Write the <see cref="WitnessActor"/> to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="WitnessActor"></param>
        public static void Write(this BinaryWriter Writer, WitnessActor WitnessActor)
        {
            Writer.Write(WitnessActor.Actor);
            Writer.Write(WitnessActor.Signature);
        }

        /// <summary>
        /// Read the <see cref="WitnessActor"/> from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static WitnessActor ReadWitnessActor(this BinaryReader Reader)
        {
            var Actor = new Actor(Reader.ReadString());
            var Signature = Reader.ReadSignSealValue();
            return new WitnessActor(Actor, Signature);
        }
    }
}
