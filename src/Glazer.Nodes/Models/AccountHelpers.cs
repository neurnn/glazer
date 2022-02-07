using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Glazer.Nodes.Models
{
    public static class AccountHelpers
    {
        /// <summary>
        /// Write the <see cref="Account"/> to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Account"></param>
        public static void Write(this BinaryWriter Writer, Account Account)
        {
            if (Account.IsValid)
            {
                Writer.Write7BitEncodedInt(1);
                Writer.Write(Account.LoginName);
                Writer.Write(Account.PublicKey);
                return;
            }

            Writer.Write7BitEncodedInt(0);
        }

        /// <summary>
        /// Read the <see cref="Account"/> from the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static Account ReadAccount(this BinaryReader Reader)
        {
            if (Reader.Read7BitEncodedInt() != 0)
            {
                var Login = Reader.ReadString();
                var PubKey = Reader.ReadSignPublicKey();
                return new Account(Login, PubKey);
            }

            return Account.Empty;
        }

        /// <summary>
        /// Convert to <see cref="JObject"/> instance.
        /// </summary>
        /// <param name="Account"></param>
        /// <returns></returns>
        public static JObject ToJson(this Account Account)
        {
            if (Account.IsValid)
            {
                var New = new JObject();

                New["login"] = Account.LoginName;
                New["pub_key"] = Account.PublicKey.ToString();

                return New;
            }

            return null;
        }

        /// <summary>
        /// Convert from <see cref="JObject"/> instance.
        /// </summary>
        /// <param name="Account"></param>
        /// <returns></returns>
        public static Account FromJson(this JObject Json)
        {
            if (Json is null)
                return Account.Empty;

            var Login = Json.Value<string>("login");
            var KeyStr = Json.Value<string>("pub_key");

            if (string.IsNullOrWhiteSpace(Login))
                return Account.Empty;

            if (!SignPublicKey.TryParse(KeyStr, out var Key))
                return Account.Empty;

            return new Account(Login, Key);
        }
    }
}
