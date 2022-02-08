﻿using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using System;

namespace Glazer.Nodes.Records
{
    public struct HistoryRowKey : IEquatable<HistoryRowKey>
    {
        /// <summary>
        /// Null Key that points nothing.
        /// </summary>
        public static readonly HistoryRowKey Null = new HistoryRowKey();

        /// <summary>
        /// Initialize a new <see cref="HistoryRowKey"/> value.
        /// </summary>
        /// <param name="Login"></param>
        /// <param name="CodeId"></param>
        public HistoryRowKey(string Login, HashValue CodeId)
        {
            this.Login = Login;
            this.CodeId = CodeId;
        }

        /// <summary>
        /// Initialize a new <see cref="HistoryRowKey"/> value.
        /// </summary>
        /// <param name="Input"></param>
        public HistoryRowKey(string Input)
        {
            var Temp = Parse(Input);

            Login = Temp.Login;
            CodeId = Temp.CodeId;
        }

        /* Compares two hash values. */
        public static bool operator ==(HistoryRowKey L, HistoryRowKey R) => L.Equals(R);
        public static bool operator !=(HistoryRowKey L, HistoryRowKey R) => !L.Equals(R);

        /// <summary>
        /// Try to parse the <paramref name="Input"/> to <paramref name="Output"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out HistoryRowKey Output)
        {
            var Collon = Input.IndexOf(':');
            if (Collon > 0)
            {
                var Login = Input.Substring(0, Collon);
                var CodeStr = Input.Substring(Collon + 1);

                if (HashValue.TryParse(CodeStr, out var CodeId))
                {
                    Output = new HistoryRowKey(Login, CodeId);
                    return true;
                }
            }

            Output = Null;
            return false;
        }

        /// <summary>
        /// Parse the <paramref name="Input"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static HistoryRowKey Parse(string Input)
        {
            if (!TryParse(Input, out var RetVal))
                throw new FormatException("the input string is not valid.");

            return RetVal;
        }

        /// <summary>
        /// Test whether the record key is valid or not.
        /// </summary>
        public bool IsNull => string.IsNullOrWhiteSpace(Login) || !CodeId.IsValid;

        /// <summary>
        /// Test whether the column key is access to the record member or not.
        /// </summary>
        /// <param name="ColumnKey"></param>
        /// <returns></returns>
        public bool IsMemberOf(HistoryColumnKey ColumnKey) => Equals(ColumnKey.RowKey);

        /// <summary>
        /// Login Name who is owner of the record.
        /// </summary>
        public string Login { get; }

        /// <summary>
        /// Code ID that generated the record.
        /// </summary>
        public HashValue CodeId { get; }

        /// <inheritdoc/>
        public bool Equals(HistoryRowKey Other)
        {
            if (IsNull || Other.IsNull)
                return IsNull == Other.IsNull;

            if (!Login.CaseEquals(Other.Login))
                return false;

            if (CodeId != Other.CodeId)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object Obj)
        {
            if (Obj is HistoryRowKey Key)
                return Equals(Key);

            return base.Equals(Obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (IsNull)
                return HashCode.Combine(string.Empty, HashValue.Empty);

            return HashCode.Combine(Login, CodeId);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsNull)
                return string.Empty;

            return $"{Login}:{CodeId}";
        }
    }
}