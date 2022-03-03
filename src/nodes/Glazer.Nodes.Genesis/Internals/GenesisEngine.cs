using Backrole.Crypto;
using Glazer.Common.Common;
using Glazer.Common.Models;
using Glazer.Kvdb.Extensions;
using Glazer.Nodes.Abstractions;
using Glazer.P2P.Abstractions;
using Glazer.Storage.Abstraction;
using Glazer.Storage.Integration.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GActor = Glazer.Common.Models.Actor;

namespace Glazer.Nodes.Internals
{
    internal class GenesisEngine : INodeEngine
    {
        private INodeEngineManager m_Manager;
        private IStorage m_Blocks;

        private NodeOptions m_Options;
        private ILogger m_Logger;

        /// <summary>
        /// Initialize a new <see cref="GenesisEngine"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        public GenesisEngine(IServiceProvider Services)
        {
            m_Blocks = Services.GetBlockStorage();
            m_Manager = Services.GetRequiredService<INodeEngineManager>();
            m_Options = Services.GetRequiredService<NodeOptions>();
            m_Logger = Services.GetRequiredService<ILogger<GenesisEngine>>();
        }
        
        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken Token = default)
        {
            m_Logger.LogInformation("Checking the genesis block...");

            if (m_Blocks.InitialBlockId.IsValid)
            {
                m_Logger.LogError($"the genesis block is already created ({m_Blocks.InitialBlockId}).");
                m_Logger.LogWarning("switching the operating mode to multi-peer system...");
                m_Manager.SwitchTo(NodeMode.Multi);
                return;
            }

            if (string.IsNullOrWhiteSpace(m_Options.GenesisFile) ||
                !File.Exists(m_Options.GenesisFile))
            {
                m_Logger.LogCritical("no genesis settings found.");
                return;
            }

            if (GetNodeIdentity(out var Actor, out var KeyPair))
            {
                var Request = MakeGenesisRequest(out var InitialBlockId);
                if (Request is null) return;

                Request.SignAsProducer(Actor, KeyPair, true);
                var Block = Request.Harden();

                await m_Blocks.PutAsync(InitialBlockId, Block);
                
                m_Logger.LogInformation($"the genesis block created: {InitialBlockId}, {Block.Producer.Signature}");
                m_Logger.LogWarning("switching the operating mode to multi-peer system...");

                m_Manager.SwitchTo(NodeMode.Multi);
            }
        }

        /// <summary>
        /// Get the Identity of the node.
        /// </summary>
        /// <param name="Actor"></param>
        /// <param name="KeyPair"></param>
        /// <returns></returns>
        private bool GetNodeIdentity(out GActor Actor, out SignKeyPair KeyPair)
        {
            try
            {
                if (!GActor.CanUse(m_Options.Actor) || !(Actor = m_Options.Actor).IsValid)
                    throw new InvalidOperationException("invalid actor name specified.");

                if (!(KeyPair = m_Options.GetKeyPair()).IsValid)
                    throw new InvalidOperationException("invalid key pair specified.");
            }
            catch (Exception e)
            {
                m_Logger.LogCritical(e, "node options are mis-configured.");
                Actor = default; KeyPair = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Make the <see cref="BlockRequest"/> to generate a genesis block.
        /// </summary>
        /// <param name="InitialBlockId"></param>
        /// <returns></returns>
        private BlockRequest MakeGenesisRequest(out BlockId InitialBlockId)
        {
            TimeStamp InitialTimeStamp;
            Dictionary<string, string> InitialKvData;

            try
            {
                GenesisSettings Settings = JsonConvert.DeserializeObject<GenesisSettings>(
                   File.ReadAllText(m_Options.GenesisFile, Encoding.UTF8));

                if (Settings is null)
                    throw new InvalidOperationException("no valid settings loaded.");

                if ((InitialBlockId = Settings.GetInitialBlockId()).Guid == Guid.Empty)
                    throw new InvalidOperationException($"`{Guid.Empty}` is reserved to represent an invalid block id.");

                InitialTimeStamp = Settings.GetInitialTimeStamp();
                InitialKvData = Settings.InitialKvData ?? new();

            }
            catch (Exception e)
            {
                m_Logger.LogCritical(e, "failed to load the genesis settings.");
                InitialBlockId = default;
                return null;
            }

            var Request = new BlockRequest();

            Request.TimeStamp = InitialTimeStamp;
            foreach (var Each in InitialKvData)
            {
                Request.Data.Set(Each.Key, Encoding.UTF8.GetBytes(Each.Value));
            }

            return Request;
        }
    }
}
