using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Common.Models
{
    public struct ScriptId : IEquatable<ScriptId>
    {
        /// <summary>
        /// Initialize a new <see cref="ScriptId"/> instance.
        /// </summary>
        /// <param name="Guid"></param>
        public ScriptId(Guid Guid) => this.Guid = Guid;

        /* Comparison operators. */
        public static bool operator ==(ScriptId L, ScriptId R) => L.Equals(R);
        public static bool operator !=(ScriptId L, ScriptId R) => !L.Equals(R);

        /// <summary>
        /// Script Guid.
        /// </summary>
        public Guid Guid { get; }

        /// <summary>
        /// Determines the script id is valid or not.
        /// </summary>
        public bool IsValid => Guid != Guid.Empty;

        /// <inheritdoc/>
        public bool Equals(ScriptId Other)
        {
            return Guid == Other.Guid;
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is ScriptId Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => Guid.GetHashCode();
    }
}
