using System;
using System.Linq;

namespace Glazer.Common.Models
{
    /// <summary>
    /// Actor who requested an operation for the manipulating the block-chain.
    /// </summary>
    public struct Actor : IEquatable<Actor>
    {
        private static readonly string CHARS = "abcdefghijklmnopqrstuvwxyz0123456789_-.";

        /// <summary>
        /// Initialize a new <see cref="Actor"/> instance.
        /// </summary>
        /// <param name="Login"></param>
        public Actor(string Login) => this.Login = (Login ?? "").ToLower();

        /* Comparison operators. */
        public static bool operator ==(Actor L, Actor R) => L.Equals(R);
        public static bool operator !=(Actor L, Actor R) => !L.Equals(R);

        /// <summary>
        /// Test whether the login name can be used or not.
        /// </summary>
        /// <param name="Login"></param>
        /// <returns></returns>
        public static bool CanUse(string Login)
        {
            if (string.IsNullOrWhiteSpace(Login) || Login.Length > 16 || Login.Length < 4)
                return false;

            return Login.Count(X => CHARS.Contains(X)) == Login.Length;
        }

        /// <summary>
        /// Actor to String.
        /// </summary>
        /// <param name="Actor"></param>
        public static implicit operator string(Actor Actor) => Actor.Login ?? "";

        /// <summary>
        /// String to Actor.
        /// </summary>
        /// <param name="Login"></param>
        public static implicit operator Actor(string Login) => new Actor(Login);

        /// <summary>
        /// Login Name.
        /// </summary>
        public string Login { get; }

        /// <summary>
        /// Determines whether the actor is valid or not.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Login);

        /// <inheritdoc/>
        public bool Equals(Actor Other) => Login == Other.Login;

        /// <inheritdoc/>
        public override bool Equals(object Input)
        {
            if (Input is Actor Other)
                return Equals(Other);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (string.IsNullOrWhiteSpace(Login))
                return string.Empty.GetHashCode();

            return Login.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString() => Login ?? "[INVALID]";
    }
}
