using Backrole.Crypto;
using Glazer.Common.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Glazer.Common.Models
{
    public class TransactionRequest
    {
        private static readonly string[] EMPTY_STRINGS = new string[0];

        private List<Actor> m_Receivers;
        private JArray m_Arguments;

        /// <summary>
        /// Sender.
        /// </summary>
        public Actor Sender { get; set; }

        /// <summary>
        /// Signature of the sender.
        /// </summary>
        public SignSealValue Signature { get; set; }

        /// <summary>
        /// Receivers.
        /// </summary>
        public List<Actor> Receivers
        {
            get => ModelHelpers.OnDemand(ref m_Receivers);
            set => m_Receivers = value;
        }

        /// <summary>
        /// Time Stamp.
        /// </summary>
        public TimeStamp TimeStamp { get; set; } = TimeStamp.Now;

        /// <summary>
        /// Script Action.
        /// </summary>
        public ScriptAction Action { get; set; }

        /// <summary>
        /// Script Arguments.
        /// </summary>
        public JArray Arguments
        {
            get => ModelHelpers.OnDemand(ref m_Arguments);
            set => m_Arguments = value;
        }

        /// <summary>
        /// Import the transaction request from binary reader.
        /// </summary>
        /// <param name="Reader"></param>
        public void Import(BinaryReader Reader)
        {
            if (Reader.Read7BitEncodedInt() > Transaction.CurrentVersion)
                throw new NotSupportedException("not supported version.");

            Sender = Reader.ReadString();
            Signature = Reader.ReadSignSealValue();
            Receivers.Clear();

            var Count = Reader.Read7BitEncodedInt();
            for (var i = 0; i < Count; ++i)
                Receivers.Add(Reader.ReadString());

            TimeStamp = Reader.ReadTimeStamp();

            var Script = new ScriptId(Reader.ReadGuid());
            var ScriptName = Reader.ReadString();
            Action = new ScriptAction(Script, ScriptName);

            var Bson = Reader.ReadBytes(Reader.Read7BitEncodedInt());
            Arguments = BsonConvert.Deserialize<JArray>(Bson);
        }

        /// <summary>
        /// Export the transaction request to binary writer.
        /// </summary>
        /// <param name="Writer"></param>
        public void Export(BinaryWriter Writer)
        {
            Writer.Write7BitEncodedInt(Transaction.CurrentVersion);

            Writer.Write(Sender);
            Writer.Write(Signature);

            Writer.Write7BitEncodedInt(Receivers.Count);
            foreach (var Each in Receivers)
                Writer.Write(Each);

            Writer.Write(TimeStamp);
            Writer.Write(Action.Script.Guid);
            Writer.Write(Action.Action ?? "");

            var Args = BsonConvert.Serialize(Arguments);
            Writer.Write7BitEncodedInt(Args.Length);
            Writer.Write(Args);
        }

        /// <summary>
        /// Export the transaction request to <see cref="JObject"/>
        /// </summary>
        /// <param name="Json"></param>
        public void Export(JObject Json)
        {
            Json["timestamp"] = TimeStamp.Value;
            Json["sender"] = ModelHelpers.Export(WitnessActor.TryExport, new WitnessActor(Sender, Signature));

            var Receivers = new JArray();
            foreach (var Each in this.Receivers)
                Receivers.Add(Each);

            Json["receivers"] = Receivers;
            Json["script"] = ModelHelpers.Export(ScriptAction.TryExport, Action);
            Json["arguments"] = Arguments;
        }

        /// <summary>
        /// Import the transaction request from <see cref="JObject"/>
        /// </summary>
        /// <param name="Json"></param>
        public void Import(JObject Json)
        {
            var Sender = Json.Value<JObject>("sender").Import<WitnessActor>(WitnessActor.TryImport);

            TimeStamp = new TimeStamp(Json.Value<long>("timestamp"));
            
            this.Sender = Sender.Actor;
            Signature = Sender.Signature;

            Receivers = new List<Actor>((Json.Value<string[]>("receivers") ?? EMPTY_STRINGS).Select(X => new Actor(X)).ToArray());
            Action = Json.Value<JObject>("script").Import<ScriptAction>(ScriptAction.TryImport);
            Arguments = Json.Value<JArray>("arguments") ?? new JArray();
        }

        /// <summary>
        /// Make the transaction id.
        /// </summary>
        /// <returns></returns>
        public HashValue CalculateTransactionId()
        {
            using var Writer = new PacketWriter(); Export(Writer);
            return Hashes.Default.Hash("SHA256", Writer.ToByteArray());
        }

        /// <summary>
        /// Sign the transaction request using the specified <see cref="SignKeyPair"/> value.
        /// </summary>
        /// <param name="KeyPair"></param>
        /// <returns></returns>
        public void Sign(SignKeyPair KeyPair, bool Enforce = false)
        {
            if (!Enforce && Signature.IsValid)
                throw new InvalidOperationException("the transaction request has been signed.");

            using var Writer = new PacketWriter();
            Signature = SignSealValue.Empty;
            Export(Writer);

            Signature = KeyPair.SignSeal(Writer.ToByteArray());
        }

        /// <summary>
        /// Verify the transaction request using the written informations.
        /// </summary>
        /// <returns></returns>
        public bool Verify()
        {
            var Signature = this.Signature;
            if (Signature.IsValid)
            {
                using var Writer = new PacketWriter();
                this.Signature = SignSealValue.Empty;
                Export(Writer);

                this.Signature = Signature;
                return Signature.Verify(Writer.ToByteArray());
            }

            return false;
        }

        /// <summary>
        /// Harden the <see cref="TransactionRequest"/> to <see cref="Transaction"/> value.
        /// </summary>
        /// <returns></returns>
        public Transaction Harden()
        {
            if (!Signature.IsValid)
                throw new InvalidOperationException("No transaction signed.");

            if (!Verify())
                throw new InvalidOperationException("No transaction passed the verification.");

            return new Transaction(
                CalculateTransactionId(), new WitnessActor(Sender, Signature),
                Receivers.ToArray(), TimeStamp, Action, Arguments);
        }
    }
}
