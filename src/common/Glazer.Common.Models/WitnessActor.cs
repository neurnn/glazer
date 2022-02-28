using Backrole.Crypto;
using Newtonsoft.Json.Linq;
using System;

namespace Glazer.Common.Models
{
    /// <summary>
    /// Witness information.
    /// </summary>
    public struct WitnessActor : IEquatable<WitnessActor>, IEquatable<Actor>
    {
        /// <summary>
        /// Empty WitnessActors.
        /// </summary>
        public static readonly WitnessActor[] Empty = new WitnessActor[0];

        /// <summary>
        /// Initialize a new <see cref="WitnessActor"/> instance.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="Signature"></param>
        public WitnessActor(Actor Actor, SignSealValue Signature)
        {
            this.Actor = Actor;
            this.Signature = Signature;
        }

        /* Comparison operators. */
        public static bool operator ==(WitnessActor L, WitnessActor R) => L.Equals(R);
        public static bool operator !=(WitnessActor L, WitnessActor R) => !L.Equals(R);

        /// <summary>
        /// Try to export the <see cref="WitnessActor"/> to <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Actor"></param>
        /// <returns></returns>
        public static bool TryExport(JObject Json, WitnessActor Actor)
        {
            if (Actor.Signature.IsValid)
            {
                Json["login"] = Actor.Actor.Login;
                Json["pub_key"] = Actor.Signature.PublicKey.ToString();
                Json["signature"] = Actor.Signature.Signature.ToString();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to import the <see cref="WitnessActor"/> from <see cref="JObject"/>.
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="Actor"></param>
        /// <returns></returns>
        public static bool TryImport(JObject Json, out WitnessActor Actor)
        {
            var Login = Json.Value<string>("login");
            var PubKeyStr = Json.Value<string>("pub_key");
            var SignatureStr = Json.Value<string>("signature");

            if (!string.IsNullOrWhiteSpace(Login) &&
                !string.IsNullOrWhiteSpace(PubKeyStr) &&
                !string.IsNullOrWhiteSpace(SignatureStr) &&
                SignPublicKey.TryParse(PubKeyStr, out var PubKey) &&
                SignValue.TryParse(SignatureStr, out var Sign))
            {
                Actor = new WitnessActor(Login, new SignSealValue(Sign, PubKey));
                return true;
            }

            Actor = default;
            return false;
        }

        /// <summary>
        /// Actor.
        /// </summary>
        public Actor Actor { get; }

        /// <summary>
        /// Actor's Signature.
        /// </summary>
        public SignSealValue Signature { get; }

        /// <summary>
        /// Determinse whether the <see cref="WitnessActor"/> is valid or not.
        /// </summary>
        public bool IsValid => Actor.IsValid && Signature.IsValid;

        /// <inheritdoc/>
        public bool Equals(Actor Other) => Actor == Other;

        /// <inheritdoc/>
        public bool Equals(WitnessActor Other) => Actor == Other.Actor && Signature == Other.Signature;

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is Actor Actor)
                return Equals(Actor);

            if (Input is WitnessActor Witness)
                return Equals(Witness);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Actor, Signature);

        /// <inheritdoc/>
        public override string ToString()
            => Actor.IsValid && Signature.IsValid
            ? $"{Actor}@{Signature}" : "[INVALID]";
    }
}
