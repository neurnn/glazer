using Backrole.Crypto;
using Glazer.Accessors.Abstractions;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Accessors
{
    public class AccountAccessor : DataAccessor<AccountAbi>
    {
        private static readonly ScriptId SCRIPT_ID = new ScriptId(new Guid(Identity));
        private static readonly string LOGIN_CHARACTORS = "abcdefghijklmnopqrstuvwxyz0123456789-_.";
        private ProtocolAccessor m_Protocols;

        /// <summary>
        /// Identity of the account contract.
        /// </summary>
        public const string Identity = "c4117c60-f3cd-49ad-a948-01d32ddf0f16";

        /// <summary>
        /// Initialize a new <see cref="AccountAccessor"/> instance.
        /// </summary>
        /// <param name="ScriptId"></param>
        /// <param name="SurfaceSet"></param>
        /// <param name="CaptureSet"></param>
        public AccountAccessor(IKvTable SurfaceSet, IKvTable CaptureSet = null) 
            : base(SCRIPT_ID, SurfaceSet, CaptureSet)
        {
            m_Protocols = new ProtocolAccessor(SurfaceSet, CaptureSet);
        }

        /// <summary>
        /// Test whether the login name is suitable for the blockchain or not.
        /// </summary>
        /// <param name="Login"></param>
        /// <returns></returns>
        public static bool IsSuitable(string Login)
        {
            if (string.IsNullOrWhiteSpace(Login))
                return false;

            if (Login.Count(X => LOGIN_CHARACTORS.Contains(X)) != Login.Length)
                return false;

            return true;
        }

        /// <summary>
        /// Create an account asynchronously.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="PublicKey"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> CreateAsync(Actor Actor, SignPublicKey PublicKey, CancellationToken Token = default)
        {
            if (!IsSuitable(Actor))
                return false;

            if (!(await GetDataAsync(Actor, Token)).HasValue)
            {
                var Account = new AccountAbi(Actor);
                Account.PublicKeys.Add(PublicKey);

                return await SetDataAsync(Actor, Account, Token);
            }

            return false;
        }

        /// <summary>
        /// Get the <see cref="AccountAbi"/> of the <see cref="Actor"/>.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public Task<AccountAbi?> GetAsync(Actor Actor, CancellationToken Token = default) => GetDataAsync(Actor, Token);

        /// <summary>
        /// Verify the actor and its signature is valid for accessing the account.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="Signature"></param>
        /// <param name="Digest"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> VerifyAsync(Actor Actor, SignSealValue Signature, byte[] Digest, CancellationToken Token = default)
        {
            var Account = await GetAsync(Actor, Token);
            if (Account.HasValue && Account.Value.PublicKeys.Contains(Signature.PublicKey))
            {
                return Signature.Verify(Digest);
            }

            return false;
        }

        /// <summary>
        /// Verify the actor and its signature is valid for accessing the account.
        /// </summary>
        /// <param name="WitnessActor"></param>
        /// <param name="Digest"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public Task<bool> VerifyAsync(WitnessActor WitnessActor, byte[] Digest, CancellationToken Token = default)
        {
            return VerifyAsync(WitnessActor.Actor, WitnessActor.Signature, Digest, Token);
        }

        /// <summary>
        /// Add the <see cref="SignPublicKey"/> to <see cref="AccountAbi"/>.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="PublicKey"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> AddKeyAsync(Actor Actor, SignPublicKey PublicKey, CancellationToken Token = default)
        {
            var Account = await GetAsync(Actor, Token);
            if (Account.HasValue && !Account.Value.PublicKeys.Contains(PublicKey))
            {
                Account.Value.PublicKeys.Add(PublicKey);
                return await SetDataAsync(Actor, Account, Token);
            }

            return false;
        }

        /// <summary>
        /// Remove the <see cref="SignPublicKey"/> from <see cref="AccountAbi"/>.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="PublicKey"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> RemoveKeyAsync(Actor Actor, SignPublicKey PublicKey, CancellationToken Token = default)
        {
            var Account = await GetAsync(Actor, Token);
            if (Account.HasValue && Account.Value.PublicKeys.Remove(PublicKey))
                return await SetDataAsync(Actor, Account, Token);

            return false;
        }

        /// <inheritdoc/>
        public override async Task<bool> SetDataAsync(string Actor, AccountAbi? Data, CancellationToken Token = default)
        {
            var Protocol = await m_Protocols.GetAsync(SCRIPT_ID, Token);
            if (Protocol.IsValid)
            {
                throw new InvalidOperationException(
                    $"Account protocol has been activated. " +
                    $"please request to create account using the account contract ({SCRIPT_ID}).");
            }

            return await base.SetDataAsync(Actor, Data, Token);
        }
    }
}
