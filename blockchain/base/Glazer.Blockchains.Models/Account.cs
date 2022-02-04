using Glazer.Blockchains.Models.Interfaces;
using Glazer.Blockchains.Models.Internals;
using Glazer.Core.Cryptography;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models
{
    public class Account : IEncodable, IEncodeToJObject, IDecodeFromJObject
    {
        /// <summary>
        /// Login of the account. 
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Creation Time of the account.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Latest Modified Time of the account.
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// Public Keys to allow to authorize the system.
        /// </summary>
        public List<PublicKey> PublicKeys { get; set; }

        /// <summary>
        /// Permission-Sets. <br />
        /// Format: [code-kind/judgement]<br />
        /// e.g. "pb:account.new/deny".<br />
        /// e.g. "pb:account.modify/allow".<br />
        /// e.g. "ref:[CODE GUID]:obj.func/allow".<br />
        /// </summary>
        public List<string> Permissions { get; set; }

        /// <summary>
        /// Encode the account to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Options"></param>
        public void Encode(BinaryWriter Writer, NodeOptions Options)
        {
            Writer.Write(Login);
            Writer.Write(CreationTime.ToSeconds(Options.Epoch));
            Writer.Write(ModifiedTime.ToSeconds(Options.Epoch));

            if (PublicKeys is null)
                Writer.Write(0);

            else
            {
                Writer.Write(PublicKeys.Count);
                foreach (var Each in PublicKeys)
                    Writer.Write(Each.ToString());
            }

            if (Permissions is null)
                Writer.Write(0);

            else
            {
                Writer.Write(Permissions.Count);
                foreach (var Each in Permissions)
                    Writer.Write(Each);
            }
        }

        /// <summary>
        /// Decode the account from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="Options"></param>
        public void Decode(BinaryReader Reader, NodeOptions Options)
        {
            Login = Reader.ReadString();
            CreationTime = Reader.ReadDouble().ToDateTime(Options.Epoch);
            ModifiedTime = Reader.ReadDouble().ToDateTime(Options.Epoch);

            PublicKeys = new List<PublicKey>();
            Permissions = new List<string>();

            var KeyLength = Reader.ReadInt32();
            for (var i = 0; i < KeyLength; ++i)
                PublicKeys.Add(PublicKey.Parse(Reader.ReadString()));

            var PermLength = Reader.ReadInt32();
            for (var i = 0; i < PermLength; ++i)
                Permissions.Add(Reader.ReadString());
        }

        /// <summary>
        /// Encode the account to the <see cref="JObject"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Options"></param>
        public JObject EncodeToJObject(NodeOptions Options)
        {
            var New = new JObject();

            New["login"] = Login;
            New["creation_time"] = CreationTime.ToSeconds(Options.Epoch);
            New["modified_time"] = ModifiedTime.ToSeconds(Options.Epoch);

            var Pubs = new JArray();
            if (PublicKeys != null)
            {
                foreach (var Each in PublicKeys)
                    Pubs.Add(Each.ToString());
            }

            var Perms = new JArray();
            if(Permissions != null)
            {
                foreach (var Each in Permissions)
                    Perms.Add(Each);
            }

            New["public_keys"] = Pubs;
            New["permissions"] = Pubs;

            return New;
        }

        /// <summary>
        /// Decode the account from the <see cref="JObject"/>.
        /// </summary>
        /// <param name="JObject"></param>
        /// <param name="Options"></param>
        public void DecodeFromJObject(JObject JObject, NodeOptions Options)
        {
            Login = JObject.Value<string>("login");
            CreationTime = JObject.Value<double>("creation_time").ToDateTime(Options.Epoch);
            ModifiedTime = JObject.Value<double>("modified_time").ToDateTime(Options.Epoch);

            var Pubs = JObject.Value<JArray>("public_keys");
            var Perms = JObject.Value<JArray>("permissions");

            PublicKeys = new List<PublicKey>();
            Permissions = new List<string>();

            for (var i = 0; i < Pubs.Count; ++i)
                PublicKeys.Add(PublicKey.Parse(Pubs[i].Value<string>()));

            for (var i = 0; i < Perms.Count; ++i)
                Permissions.Add(Perms[i].Value<string>());
        }
    }
}
