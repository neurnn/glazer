using Backrole.Crypto;
using Glazer.Common.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Common.Models
{
    /// <summary>
    /// Irresible Transaction information.
    /// </summary>
    public struct Transaction : IEquatable<Transaction>
    {
        private static readonly JArray EMPTY_ARRAY = new JArray();
        private static readonly string[] EMPTY_STRINGS = new string[0];

        /// <summary>
        /// Version information.
        /// </summary>
        public const int CurrentVersion = 0;

        /// <summary>
        /// Empty Transactions.
        /// </summary>
        public static readonly Transaction[] Empty = new Transaction[0];

        /// <summary>
        /// Initialize a new <see cref="Transaction"/> instance.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Sender"></param>
        /// <param name="Receivers"></param>
        /// <param name="TimeStamp"></param>
        /// <param name="Action"></param>
        /// <param name="Arguments"></param>
        internal Transaction(
            HashValue Id, WitnessActor Sender, 
            Actor[] Receivers, TimeStamp TimeStamp, ScriptAction Action, JArray Arguments)
        {
            this.Id = Id;
            this.Sender = Sender;
            this.Receivers = Receivers;
            this.TimeStamp = TimeStamp;
            this.Action = Action;
            this.Arguments = Arguments;
        }

        /* Comparison operators. */
        public static bool operator ==(Transaction L, Transaction R) => L.Equals(R);
        public static bool operator !=(Transaction L, Transaction R) => !L.Equals(R);

        /// <summary>
        /// Try to import the hardened transaction.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static bool TryImport(BinaryReader Reader, out Transaction Transaction)
        {
            try
            {
                if (Reader.Read7BitEncodedInt() > CurrentVersion)
                {
                    Transaction = default;
                    return false;
                }

                var Id = Reader.ReadHashValue();
                var Sender = Reader.ReadWitnessActor();
                var Signature = Reader.ReadSignSealValue();
                var Receivers = new Actor[Reader.Read7BitEncodedInt()];
                for (var i = 0; i < Receivers.Length; ++i)
                    Receivers[i] = Reader.ReadString();

                var TimeStamp = Reader.ReadTimeStamp();

                var Script = new ScriptId(Reader.ReadGuid());
                var ScriptName = Reader.ReadString();
                var Action = new ScriptAction(Script, ScriptName);

                var Bson = Reader.ReadBytes(Reader.Read7BitEncodedInt());
                var Arguments = BsonConvert.Deserialize<JArray>(Bson);

                if (!Id.IsValid)
                    return ModelHelpers.Return(false, out Transaction);

                if (!Sender.IsValid)
                    return ModelHelpers.Return(false, out Transaction);

                if (!Signature.IsValid)
                    return ModelHelpers.Return(false, out Transaction);

                if (!Action.IsValid)
                    return ModelHelpers.Return(false, out Transaction);

                foreach (var Each in Receivers)
                {
                    if (!Each.IsValid)
                        return ModelHelpers.Return(false, out Transaction);
                }

                Transaction = new Transaction(Id, Sender, Receivers, TimeStamp, Action, Arguments);
                return true;
            }
            catch { }

            Transaction = default;
            return false;
        }

        /// <summary>
        /// Try to export the hardened transaction.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Transaction"></param>
        /// <returns></returns>
        public static bool TryExport(BinaryWriter Writer, Transaction Transaction)
        {
            try
            {
                if (!Transaction.Id.IsValid)
                    return false;

                if (!Transaction.Sender.IsValid)
                    return false;

                if (!Transaction.Action.IsValid)
                    return false;

                foreach (var Each in Transaction.Receivers)
                {
                    if (!Each.IsValid)
                        return false;
                }

                var Bson = BsonConvert.Serialize(Transaction.Arguments);

                Writer.Write7BitEncodedInt(CurrentVersion); // ==> Version.

                Writer.Write(Transaction.Id);
                Writer.Write(Transaction.Sender);

                Writer.Write7BitEncodedInt(Transaction.Receivers.Length);
                foreach (var Each in Transaction.Receivers)
                    Writer.Write(Each);

                Writer.Write(Transaction.TimeStamp);
                Writer.Write(Transaction.Action.Script.Guid);
                Writer.Write(Transaction.Action.Action ?? "");

                Writer.Write7BitEncodedInt(Bson.Length);
                Writer.Write(Bson);
                return true;
            }

            catch { }
            return false;
        }

        /// <summary>
        /// Try to export the transaction to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Transaction"></param>
        /// <returns></returns>
        public static bool TryExport(JObject Json, Transaction Transaction)
        {
            try
            {
                if (!Transaction.Id.IsValid)
                    return false;

                if (!Transaction.Sender.IsValid)
                    return false;

                if (!Transaction.Action.IsValid)
                    return false;

                foreach (var Each in Transaction.Receivers)
                {
                    if (!Each.IsValid)
                        return false;
                }

                Json["version"] = CurrentVersion;
                Json["timestamp"] = Transaction.TimeStamp.Value;

                Json["id"] = Transaction.Id.ToString();
                Json["sender"] = ModelHelpers.Export(WitnessActor.TryExport, Transaction.Sender);

                var Receivers = new JArray();
                foreach (var Each in Transaction.Receivers)
                    Receivers.Add(Each.Login);

                Json["receivers"] = Receivers;
                Json["script"] = ModelHelpers.Export(ScriptAction.TryExport, Transaction.Action);
                Json["arguments"] = Transaction.Arguments;
                return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Try to import the transaction from the <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Transaction"></param>
        /// <returns></returns>
        public static bool TryImport(JObject Json, out Transaction Transaction)
        {
            try
            {
                if (Json.Value<int>("version") > CurrentVersion)
                    return ModelHelpers.Return(false, out Transaction);

                var IdStr = Json.Value<string>("id");

                if (!HashValue.TryParse(IdStr, out var Id))
                    return ModelHelpers.Return(false, out Transaction);

                var TimeStamp = Json.Value<long>("timestamp");
                var Sender = Json.Value<JObject>("sender").Import<WitnessActor>(WitnessActor.TryImport);
                var Receivers = (Json.Value<string[]>("receivers") ?? EMPTY_STRINGS).Select(X => new Actor(X)).ToArray();
                var Script = Json.Value<JObject>("script").Import<ScriptAction>(ScriptAction.TryImport);
                var Arguments = Json.Value<JArray>("arguments");

                Transaction = new Transaction(Id, Sender, Receivers, new TimeStamp(TimeStamp), Script, Arguments);
                return true;
            }

            catch { }

            Transaction = default;
            return false;
        }

        /// <summary>
        /// Transaction Id.
        /// </summary>
        public HashValue Id { get; }

        /// <summary>
        /// Sender.
        /// </summary>
        public WitnessActor Sender { get; }

        /// <summary>
        /// Receivers.
        /// </summary>
        public Actor[] Receivers { get; }

        /// <summary>
        /// Time Stamp.
        /// </summary>
        public TimeStamp TimeStamp { get; }

        /// <summary>
        /// Script Action.
        /// </summary>
        public ScriptAction Action { get; }

        /// <summary>
        /// Script Arguments.
        /// </summary>
        public JArray Arguments { get; }

        /// <summary>
        /// Determines whether the transaction is valid or not.
        /// </summary>
        public bool IsValid => Id.IsValid && Sender.IsValid && Receivers != null && Action.IsValid && Arguments != null;

        /// <inheritdoc/>
        public bool Equals(Transaction Other)
        {
            if (Id != Other.Id)
                return false;

            if (Sender != Other.Sender)
                return false;

            if (!Receivers.SequenceEqualNullSafe(Other.Receivers))
                return false;

            if (TimeStamp != Other.TimeStamp)
                return false;

            if (Action != Other.Action)
                return false;

            return Arguments.SequenceEqualNullSafe(Other.Arguments);
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is Transaction Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Id, Sender, Receivers, Id, TimeStamp, Action, Arguments ?? EMPTY_ARRAY);

        /// <summary>
        /// Verify the transaction.
        /// </summary>
        /// <returns></returns>
        public bool Verify()
        {
            var Temp = new TransactionRequest
            {
                Sender = Sender.Actor,
                Signature = Sender.Signature,
                Receivers = new List<Actor>(Receivers),
                TimeStamp = TimeStamp,
                Action = Action,
                Arguments = Arguments
            };

            return Temp.Verify() && Id == Temp.CalculateTransactionId();
        }

        /// <summary>
        /// Make the <see cref="TransactionRequest"/> to replay.
        /// </summary>
        /// <returns></returns>
        public TransactionRequest MakeRequest() => new TransactionRequest
        {
            Sender = Sender.Actor,
            Signature = Sender.Signature,
            Receivers = new List<Actor>(Receivers),
            TimeStamp = TimeStamp,
            Action = Action,
            Arguments = Arguments
        };
    }
}
