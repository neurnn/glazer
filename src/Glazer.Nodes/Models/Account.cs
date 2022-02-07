using Backrole.Crypto;
using Backrole.Crypto.Abstractions;
using Glazer.Nodes.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Nodes.Models
{
    /// <summary>
    /// Describes a Account of the blockchain network.
    /// Note that this represents only Account information.
    /// </summary>
    public struct Account : IEquatable<Account>
    {
        /// <summary>
        /// Characters that allowed for the Login Name.
        /// </summary>
        public const string NAME_CHARS = "abcdefghijklmnopqrstuvwxyz012345678_-.";

        /// <summary>
        /// Empty Account.
        /// </summary>
        public static readonly Account Empty = new Account();

        /// <summary>
        /// Login Names.
        /// </summary>
        public struct KnownLogins
        {
            /// <summary>
            /// System Account.
            /// </summary>
            public static readonly string SYSTEM = "glazer";

            /// <summary>
            /// Token Management Account.
            /// </summary>
            public static readonly string TOKENS = "glazer.tokens";

            /// <summary>
            /// Code Management Account.
            /// </summary>
            public static readonly string CODES = "glazer.codes";
        }

        /// <summary>
        /// Initialize a new <see cref="Account"/> value.
        /// </summary>
        /// <param name="LoginName"></param>
        /// <param name="PublicKey"></param>
        public Account(string LoginName, SignPublicKey PublicKey)
        {
            this.LoginName = LoginName;
            this.PublicKey = PublicKey;
        }

        /// <summary>
        /// Initialize a new <see cref="Account"/> value.
        /// </summary>
        /// <param name="Input"></param>
        public Account(string Input)
        {
            var Temp = Parse(Input);
            LoginName = Temp.LoginName;
            PublicKey = Temp.PublicKey;
        }

        /* Comparison operators. */
        public static bool operator ==(Account L, Account R) => L.Equals(R);
        public static bool operator !=(Account L, Account R) => !L.Equals(R);

        /// <summary>
        /// Test whether the login name is valid to use on the system or not.
        /// </summary>
        /// <param name="LoginName"></param>
        /// <returns></returns>
        public static bool Check(string LoginName)
        {
            if (LoginName.IsMeaningless())
                return false;

            if (LoginName.Length <= 4 || LoginName.Length >= 24 ||
               !LoginName.ConsistedOnlyWith(NAME_CHARS))
                return false;

            return true;
        }

        /// <summary>
        /// Try to parse the <paramref name="Input"/> to <paramref name="Output"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out Account Output)
        {
            var Index = Input.IndexOf('@');
            if (Index > 0)
            {
                var Login = Input.Substring(0, Index);
                var KeyString = Input.Substring(Index + 1);
                
                if (SignPublicKey.TryParse(KeyString, out var Key))
                {
                    Output = new Account(Login, Key);
                    return true;
                }
            }

            Output = default;
            return false;
        }

        /// <summary>
        /// Parse the <paramref name="Input"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static Account Parse(string Input)
        {
            if (!TryParse(Input, out var RetVal))
                throw new FormatException($"the input string is invalid.");

            return RetVal;
        }

        /// <summary>
        /// Indicates whether the Account is valid or not.
        /// </summary>
        public bool IsValid => !LoginName.IsMeaningless() && PublicKey.IsValid;

        /// <summary>
        /// Verify the account specific seal.
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Input"></param>
        /// <param name="Provider"></param>
        /// <returns></returns>
        public bool Verify(SignValue Value, ArraySegment<byte> Input, ISignAlgorithmProvider Provider = null)
        {
            return IsValid && (Provider ?? Signs.Default).Verify(PublicKey, Value, Input);
        }

        /// <summary>
        /// Login Name.
        /// </summary>
        public string LoginName { get; }

        /// <summary>
        /// Public Key to verify the delegation is valid or not.
        /// </summary>
        public SignPublicKey PublicKey { get; }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is Account Other)
                return Equals(Other);

            return base.Equals(Input);
        }

        /// <inheritdoc/>
        public bool Equals(Account Other)
        {
            if (IsValid != Other.IsValid || !IsValid)
                return false;

            var Login = LoginName.ToLower();
            var LoginOther = Other.LoginName.ToLower();
            return Login == LoginOther && PublicKey == Other.PublicKey;
        }

        /// <summary>
        /// Returns the string expression of the Account.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IsValid)
                return $"{LoginName}@{PublicKey}";

            return string.Empty;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (IsValid)
                return HashCode.Combine(LoginName, PublicKey);

            return HashCode.Combine(string.Empty, SignPublicKey.Empty);
        }
    }
}
