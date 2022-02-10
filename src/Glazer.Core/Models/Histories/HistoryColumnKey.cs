using Backrole.Crypto;
using Glazer.Core.Helpers;
using Glazer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Records
{
    public struct HistoryColumnKey : IEquatable<HistoryColumnKey>
    {
        /// <summary>
        /// Characters that allowed for the Column Name.
        /// </summary>
        public const string NAME_CHARS = "abcdefghijklmnopqrstuvwxyz012345678_-.";

        /// <summary>
        /// Null Key that points nothing.
        /// </summary>
        public static readonly HistoryColumnKey Null = new HistoryColumnKey();

        /// <summary>
        /// Initialize a new <see cref="HistoryColumnKey"/> value.
        /// </summary>
        /// <param name="Login"></param>
        /// <param name="CodeId"></param>
        /// <param name="Column"></param>
        public HistoryColumnKey(string Login, Guid CodeId, string Column)
        {
            RowKey = new HistoryKey(Login, CodeId);
            this.Column = Column;
        }

        /// <summary>
        /// Initialize a new <see cref="HistoryColumnKey"/> value.
        /// </summary>
        /// <param name="Login"></param>
        /// <param name="CodeId"></param>
        /// <param name="Column"></param>
        public HistoryColumnKey(HistoryKey RowKey, string Column)
        {
            this.RowKey = RowKey;
            this.Column = Column;
        }

        /// <summary>
        /// Initialize a new <see cref="HistoryColumnKey"/> value.
        /// </summary>
        /// <param name="Input"></param>
        public HistoryColumnKey(string Input)
        {
            var Temp = Parse(Input);

            RowKey = Temp.RowKey;
            Column = Temp.Column;
        }

        /* Compares two hash values. */
        public static bool operator ==(HistoryColumnKey L, HistoryColumnKey R) => L.Equals(R);
        public static bool operator !=(HistoryColumnKey L, HistoryColumnKey R) => !L.Equals(R);

        /// <summary>
        /// Test whether the column name is valid to use on the system or not.
        /// </summary>
        /// <param name="Column"></param>
        /// <returns></returns>
        public static bool Check(string Column)
        {
            if (Column.IsMeaningless())
                return false;

            if (Column.Length <= 0 || Column.Length >= 24 ||
               !Column.ConsistedOnlyWith(NAME_CHARS))
                return false;

            return true;
        }

        /// <summary>
        /// Try to parse the <paramref name="Input"/> to <paramref name="Output"/>.
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="Output"></param>
        /// <returns></returns>
        public static bool TryParse(string Input, out HistoryColumnKey Output)
        {
            var Sharp = Input.IndexOf('#');
            if (Sharp > 0)
            {
                var KeyStr = Input.Substring(0, Sharp);
                var Column = Input.Substring(Sharp + 1);

                if (HistoryKey.TryParse(KeyStr, out var Key))
                {
                    Output = new HistoryColumnKey(Key, Column);
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
        public static HistoryColumnKey Parse(string Input)
        {
            if (!TryParse(Input, out var RetVal))
                throw new FormatException("the input string is not valid.");

            return RetVal;
        }

        /// <summary>
        /// Test whether the record key is valid or not.
        /// </summary>
        public bool IsNull => RowKey.IsNull || string.IsNullOrWhiteSpace(Column);

        /// <summary>
        /// Login Name who is owner of the record.
        /// </summary>
        public string Login => RowKey.Login;

        /// <summary>
        /// Code ID that generated the record.
        /// </summary>
        public Guid CodeId => RowKey.CodeId;

        /// <summary>
        /// Record Key.
        /// </summary>
        public HistoryKey RowKey { get; }

        /// <summary>
        /// Name of the record column.
        /// </summary>
        public string Column { get; }

        /// <inheritdoc/>
        public bool Equals(HistoryColumnKey Other)
        {
            if (IsNull || Other.IsNull)
                return IsNull == Other.IsNull;

            if (RowKey != Other.RowKey)
                return false;

            if (!Column.CaseEquals(Other.Column))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object Obj)
        {
            if (Obj is HistoryColumnKey Key)
                return Equals(Key);

            return base.Equals(Obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (IsNull)
                return HashCode.Combine(HistoryKey.Null, string.Empty);

            return HashCode.Combine(RowKey, Column);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsNull)
                return string.Empty;

            return $"{RowKey}#{Column}";
        }
    }
}
