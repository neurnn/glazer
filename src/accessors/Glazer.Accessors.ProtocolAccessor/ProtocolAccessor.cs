using Glazer.Accessors.Abstractions;
using Glazer.Common.Models;
using Glazer.Kvdb.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Accessors
{
    public class ProtocolAccessor : DataAccessor<ProtocolAbi>
    {
        private static readonly ScriptId SCRIPT_ID = new ScriptId(new Guid(Identity));

        /// <summary>
        /// Identity of the protocol contract.
        /// </summary>
        public const string Identity = "12b98171-d72b-4a9a-8e26-873bce59b2cc";

        /// <summary>
        /// Initialize a new <see cref="ProtocolAccessor"/> instance.
        /// </summary>
        /// <param name="SurfaceSet"></param>
        /// <param name="CaptureSet"></param>
        public ProtocolAccessor(IKvTable SurfaceSet, IKvTable CaptureSet = null)
            : base(SCRIPT_ID, SurfaceSet, CaptureSet)
        {
        }

        /// <summary>
        /// Get the protocol information asynchronously.
        /// </summary>
        /// <param name="ScriptId"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<ProtocolAbi> GetAsync(ScriptId ScriptId, CancellationToken Token = default)
        {
            var Protocol = await GetDataAsync(ScriptId.ToString(), Token);
            if (Protocol.HasValue)
            {
                return Protocol.Value;
            }

            return default;
        }

        /// <inheritdoc/>
        public override async Task<bool> SetDataAsync(string Key, ProtocolAbi? Data, CancellationToken Token = default)
        {
            var Protocol = await GetAsync(SCRIPT_ID, Token);
            if (Protocol.IsValid)
            {
                throw new InvalidOperationException(
                    $"Protocol system has been activated. " +
                    $"please request to create account using the protocol contract ({SCRIPT_ID}).");
            }

            return await base.SetDataAsync(Key, Data, Token);
        }
    }
}
