using Backrole.Crypto;
using Glazer.Nodes.Exceptions;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models;
using Glazer.Nodes.Models.Histories;
using System;
using System.IO;
using System.Text;

namespace Glazer.Nodes.Models.Transactions
{
    public static class TransactionHelpers
    {
        /// <summary>
        /// Write the <see cref="Transaction"/> to <see cref="BinaryWriter"/> with options.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Transaction"></param>
        /// <param name="Options"></param>
        public static void Write(this BinaryWriter Writer, Transaction Transaction, TransactionPackingOptions Options)
        {
            var Header = Transaction.Header;
            var Witness = Transaction.Witness;

            if (!Header.Sender.IsValid)
                throw new IncompletedException("No Sender specified.");

            if (Options.WithId && !Header.TrxId.IsValid)
                throw new IncompletedException("No TrxId set.");

            if (Options.WithSenderSeal && !Header.SenderSeal.IsValid)
                throw new IncompletedException("No Sender Seal set.");

            if (Options.WithWitness && Witness.Accounts.Count != Witness.AccountSeals.Count)
                throw new IncompletedException($"Account Seal must be pair with acoount list.");

            // --- Headers.
            Writer.Write(Header.Version);
            Writer.Write(Options.WithId ? Header.TrxId : HashValue.Empty); // Transaction Id AS EMPTY.
            Writer.Write(Header.TimeStamp);
            Writer.Write(Header.Sender);
            Writer.Write(Options.WithSenderSeal ? Header.SenderSeal : SignValue.Empty); // Sender Seal as EMPTY.

            // --- Behaviours.
            Writer.Write7BitEncodedInt(Transaction.Behaviours.Count);
            foreach (var Each in Transaction.Behaviours)
                Writer.Write(Each);

            // --- Witness Informations.
            if (!Options.WithWitness)
                Writer.Write7BitEncodedInt(0); // Accounts as EMPTY.

            else
            {
                Writer.Write7BitEncodedInt(Witness.Accounts.Count);
                for (var i = 0; i < Witness.Accounts.Count; ++i)
                {
                    Writer.Write(Witness.Accounts[i]);
                    Writer.Write(Witness.AccountSeals[i]);
                }
            }
        }

        /// <summary>
        /// Write the <see cref="TransactionBehaviour"/> to <see cref="BinaryWriter"/> with options.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Behaviour"></param>
        public static void Write(this BinaryWriter Writer, TransactionBehaviour Behaviour)
        {
            Writer.Write(Behaviour.CodeId);
            Writer.Write(Behaviour.CodeName);
            Writer.WriteFrame(Behaviour.CodeArgs);

            // --- Code Execution Expectations.
            Writer.Write7BitEncodedInt(Behaviour.CodeExpects.Count);
            foreach (var Expect in Behaviour.CodeExpects)
            {
                if (Expect.Key.IsNull)
                    continue;

                Writer.Write(Expect.Key);
                Writer.WriteFrame(Expect.Value);
            }
        }

        /// <summary>
        /// Read the <see cref="Transaction"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Recycle"></param>
        /// <returns></returns>
        public static Transaction ReadTransaction(this BinaryReader Reader, Transaction Recycle = null)
        {
            var Trx = Recycle ?? new Transaction();
            var Header = Trx.Header;
            var Witness = Trx.Witness;

            Trx.Behaviours.Clear();
            Witness.Accounts.Clear();
            Witness.AccountSeals.Clear();

            // --- Headers.
            Header.Version = Reader.ReadUInt32();
            Header.TrxId = Reader.ReadHashValue();
            Header.TimeStamp = Reader.ReadDateTime();
            Header.Sender = Reader.ReadAccount();
            Header.SenderSeal = Reader.ReadSignValue();

            // --- Behaviours.
            var BehaviourLength = Reader.Read7BitEncodedInt();
            for (var i = 0; i < BehaviourLength; ++i)
                Trx.Behaviours.Add(Reader.ReadTransactionBehaviour());

            // --- Witness Informations.
            var WitnessLength = Reader.Read7BitEncodedInt();
            for (var i = 0; i < WitnessLength; ++i)
            {
                var Account = Reader.ReadAccount();
                var AccountSeal = Reader.ReadSignValue();

                Witness.Accounts.Add(Account);
                Witness.AccountSeals.Add(AccountSeal);
            }

            return Trx;
        }

        /// <summary>
        /// Read the <see cref="TransactionBehaviour"/> from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static TransactionBehaviour ReadTransactionBehaviour(this BinaryReader Reader)
        {
            var Behaviour = new TransactionBehaviour();
            Behaviour.CodeId = Reader.ReadHashValue();
            Behaviour.CodeName = Reader.ReadString();
            Behaviour.CodeArgs = Reader.ReadFrame();

            // --- Code Execution Expectations.
            var CodeLength = Reader.Read7BitEncodedInt();
            for (var j = 0; j < CodeLength; ++j)
            {
                var Key = Reader.ReadHistoryColumnKey();
                var Value = Reader.ReadFrame();
                Behaviour.CodeExpects[Key] = Value;
            }

            return Behaviour;
        }

        /// <summary>
        /// Calculate <see cref="HashValue"/> of the <see cref="Transaction"/>.
        /// </summary>
        /// <param name="Transaction"></param>
        /// <returns></returns>
        public static HashValue MakeHashValue(this Transaction Transaction, TransactionPackingOptions Options = default)
        {
            using var Stream = new MemoryStream();
            using (var Writer = new BinaryWriter(Stream, Encoding.UTF8, true))
                Write(Writer, Transaction, Options);

            Stream.Position = 0;
            return Hashes.Default.Hash("SHA256", Stream);
        }

        /// <summary>
        /// Make Transaction Id value for <see cref="TransactionHeader.TrxId"/>.
        /// </summary>
        /// <param name="Transaction"></param>
        /// <returns></returns>
        public static HashValue MakeTrxId(this Transaction Transaction) 
            => Transaction.MakeHashValue(TransactionPackingOptions.TrxId);

        /// <summary>
        /// Makes the SenderSeal value for <see cref="TransactionHeader.SenderSeal"/>.
        /// </summary>
        /// <param name="Transaction"></param>
        /// <param name="PrivateKey"></param>
        /// <returns></returns>
        public static SignValue MakeSenderSeal(this Transaction Transaction, SignPrivateKey PrivateKey)
        {
            var Header = Transaction.Header;
            if (Header.TrxId != Transaction.MakeTrxId())
                throw new PreconditionFailedException($"TrxId is invalid.");

            var Sign = Signs.Default.Sign(PrivateKey, Header.TrxId.Value);
            if (!Signs.Default.Verify(Header.Sender.PublicKey, Sign, Header.TrxId.Value))
                throw new PreconditionFailedException($"Sender's PublicKey can not verify the message with the given private key.");

            return Sign;
        }

        /// <summary>
        /// Makes the WitnessSeal value for <see cref="TransactionWitness.AccountSeals"/>.
        /// </summary>
        /// <param name="Transaction"></param>
        /// <param name="PrivateKey"></param>
        /// <returns></returns>
        public static SignValue MakeWitnessSeal(this Transaction Transaction, Account Account, SignPrivateKey PrivateKey)
        {
            var Header = Transaction.Header;
            if (Header.TrxId != Transaction.MakeTrxId())
                throw new PreconditionFailedException($"TrxId is invalid.");

            var Hash = Transaction.MakeHashValue(TransactionPackingOptions.Witness);
            var Sign = Signs.Default.Sign(PrivateKey, Hash.Value);
            if (!Signs.Default.Verify(Account.PublicKey, Sign, Hash.Value))
                throw new PreconditionFailedException($"Account's PublicKey can not verify the message with the given private key.");

            return Sign;
        }

        /// <summary>
        /// Test the transaction is valid or not. 
        /// This does NOT guarantee whether the transaction is correctly executed or not.
        /// Nodes who has records and indices should verify its execution result meets or not, including the account's permissions.
        /// </summary>
        /// <param name="Transaction"></param>
        /// <param name="Precondition"></param>
        /// <returns></returns>
        public static bool TestValidity(this Transaction Transaction, TransactionPackingOptions? Precondition = null)
        {
            var Header = Transaction.Header;
            var Options = Precondition.HasValue 
                ? Precondition.Value : TransactionPackingOptions.Witness;

            Options.WithId = Options.WithId || Options.WithSenderSeal || Options.WithWitness;
            Options.WithSenderSeal = Options.WithSenderSeal || Options.WithWitness;

            if (Options.WithId && Transaction.MakeTrxId() != Header.TrxId)
                return false;

            if (Options.WithSenderSeal &&
               !Signs.Default.Verify(Header.Sender.PublicKey, Header.SenderSeal, Header.TrxId.Value))
                return false;

            if (Options.WithWitness)
            {
                var Witness = Transaction.Witness;
                if (Witness.Accounts.Count != Witness.AccountSeals.Count)
                    return false;

                var Hash = Transaction.MakeHashValue(TransactionPackingOptions.Witness);
                for (var i = 0; i < Witness.Accounts.Count; ++i)
                {
                    var Account = Witness.Accounts[i];
                    var AccountSeal = Witness.AccountSeals[i];

                    if (!Signs.Default.Verify(Account.PublicKey, AccountSeal, Hash.Value))
                        return false;
                }
            }

            return true;
        }
    }
}
