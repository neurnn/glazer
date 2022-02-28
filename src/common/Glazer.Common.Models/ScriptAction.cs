using Newtonsoft.Json.Linq;
using System;

namespace Glazer.Common.Models
{
    public struct ScriptAction : IEquatable<ScriptAction>
    {
        /// <summary>
        /// Initialize a new <see cref="ScriptAction"/> instance.
        /// </summary>
        /// <param name="Script"></param>
        /// <param name="Action"></param>
        public ScriptAction(ScriptId Script, string Action)
        {
            this.Script = Script;
            this.Action = (Action ?? "").ToLower();
        }

        /* Comparison operators. */
        public static bool operator ==(ScriptAction L, ScriptAction R) => L.Equals(R);
        public static bool operator !=(ScriptAction L, ScriptAction R) => !L.Equals(R);

        /// <summary>
        /// Try to export <see cref="ScriptAction"/> to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static bool TryExport(JObject Json, ScriptAction Action)
        {
            if (Action.IsValid)
            {
                Json["id"] = Action.Script.ToString();
                Json["action"] = Action.Action;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to import <see cref="ScriptAction"/> from <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Action"></param>
        /// <returns></returns>
        public static bool TryImport(JObject Json, out ScriptAction Action)
        {
            var ScriptStr = Json.Value<string>("id");
            var ActionStr = Json.Value<string>("action");

            if (string.IsNullOrWhiteSpace(ScriptStr))
                return ModelHelpers.Return(false, out Action);

            if (string.IsNullOrWhiteSpace(ActionStr))
                return ModelHelpers.Return(false, out Action);

            if (!Guid.TryParse(ScriptStr, out var Script))
                return ModelHelpers.Return(false, out Action);

            Action = new ScriptAction(new ScriptId(Script), ActionStr);
            return true;
        }

        /// <summary>
        /// Script Id.
        /// </summary>
        public ScriptId Script { get; }

        /// <summary>
        /// Script Action Name.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Determines the script action is valid or not.
        /// </summary>
        public bool IsValid => Script.IsValid && !string.IsNullOrWhiteSpace(Action);

        /// <inheritdoc/>
        public bool Equals(ScriptAction Other)
        {
            if (Script != Other.Script)
                return false;

            if (Action != Other.Action)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is ScriptAction Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (string.IsNullOrWhiteSpace(Action))
                return HashCode.Combine(Script, string.Empty);

            return HashCode.Combine(Script, Action);
        }
    }
}
