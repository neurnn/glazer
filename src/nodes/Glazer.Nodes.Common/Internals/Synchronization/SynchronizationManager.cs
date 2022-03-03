using Backrole.Crypto;
using Glazer.Common;
using Glazer.Common.Models;
using Glazer.Nodes.Abstractions;
using Glazer.Nodes.Common.Protocols;
using Glazer.P2P.Abstractions;
using Glazer.Storage.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Nodes.Common.Internals.Synchronization
{
    internal class SynchronizationManager : INodeSynchronizationManager
    {
        private static readonly byte[] EMPTY_DATA = new byte[0];

        private bool m_Enabled = false;

        private ILogger m_Logger;
        private IMessanger m_Messanger;
        private IStorage m_Storage;

        /// <summary>
        /// Token that is <see cref="INodeLifetime.Stopping"/>.
        /// </summary>
        private CancellationToken m_Token;
        private const string VOTE_SUBJECT_BLOCK = "block.sync.x";
        private const string VOTE_SUBJECT_INITIAL = "block.sync.y";
        private const string VOTE_SUBJECT_LATEST = "block.sync.z";

        /// <summary>
        /// Initialize a new <see cref="SynchronizationManager"/> instance.
        /// </summary>
        /// <param name="Storage"></param>
        public SynchronizationManager(
            INodeLifetime Lifetime, IStorage Storage, IMessanger Messanger, 
            ILogger<SynchronizationManager> Logger, INodeElectionManager Voting)
        {
            m_Token = Lifetime.Stopping;
            m_Storage = Storage; m_Messanger = Messanger;
            m_Logger = Logger;

            Voting.Subscribe(OnVoting);
        }

        /// <summary>
        /// Indicates whether the synchronization is enabled or not.
        /// </summary>
        public bool Enabled => m_Enabled;

        /// <summary>
        /// Set the enabled value to be specified <paramref name="Value"/>.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public INodeSynchronizationManager SetEnabled(bool Value)
        {
            m_Enabled = Value;
            return this;
        }

        /// <summary>
        /// Called when the voting session arrived.
        /// </summary>
        /// <param name="Vote"></param>
        /// <returns></returns>
        private async Task OnVoting(INodeElectionVote Vote)
        {
            if (!m_Enabled)
                return;

            if (Vote.Subject == VOTE_SUBJECT_BLOCK)
            {
                if (Vote.Data is null || Vote.Data.Length != 16)
                    return;

                /* Vote about the request. */
                await SubmitBlockVote(new BlockId(new Guid(Vote.Data)), Vote);
            }

            else if (Vote.Subject == VOTE_SUBJECT_INITIAL)
            {
                /* Vote about the request. */
                await SubmitBlockVote(m_Storage.InitialBlockId, Vote);
            }

            else if (Vote.Subject == VOTE_SUBJECT_LATEST)
            {
                /* Vote about the request. */
                await SubmitBlockVote(m_Storage.LatestBlockId, Vote);
            }
        }

        /// <summary>
        /// Submit a block to vote.
        /// </summary>
        /// <param name="Vote"></param>
        /// <returns></returns>
        private async Task SubmitBlockVote(BlockId BlockId, INodeElectionVote Vote)
        {
            try
            {
                var Block = await m_Storage.GetAsync(BlockId, m_Token);
                using (var Writer = new PacketWriter())
                {
                    Writer.Write(BlockId.Guid);
                    Writer.Write7BitEncodedInt(BlockId.IsValid ? 1 : 0);

                    if (BlockId.IsValid && !Block.TryExport(Writer, Block))
                        return;

                    if (!BlockId.IsValid) // --> Synchronize only the block is not found.
                        _ = SynchronizeFromOthers(BlockId, Vote);

                    /* Submit the exported block. */
                    Vote.Submit(Writer.ToByteArray());
                }
            }

            catch (Exception e)
            {
                m_Logger.LogError(e, $"Failed to submit a block: {BlockId}");
            }
        }

        /// <summary>
        /// Synchronize the block by hooking other's voting result.
        /// </summary>
        /// <param name="BlockId"></param>
        /// <param name="Vote"></param>
        /// <returns></returns>
        private async Task SynchronizeFromOthers(BlockId BlockId, INodeElectionVote Vote)
        {
            try
            {
                var Summary = await Vote.SummarizeAsync(m_Token);
                await HandleVotingSummary(BlockId, Summary, m_Token);
            }

            catch (Exception e)
            {
                m_Logger.LogError(e, $"Failed to synchronize the block, {BlockId}.");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RequestAsync(BlockId BlockId, CancellationToken Token = default)
        {
            if (m_Enabled)
            {
                var Block = await m_Storage.GetAsync(BlockId, m_Token);
                if (!Block.IsValid) // --> Synchronize only the block is not found.
                {
                    var Summary = await m_Messanger
                        .IssueAndWaitAsync(VOTE_SUBJECT_BLOCK, BlockId.Guid.ToByteArray(), 15, Token);

                    return await HandleVotingSummary(BlockId, Summary, Token);
                }

                return true;
            }

            throw new InvalidOperationException("the synchronization manager is not enabled.");
        }

        /// <summary>
        /// Handle the voting summary.
        /// </summary>
        /// <param name="BlockId"></param>
        /// <param name="Summary"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task<bool> HandleVotingSummary(BlockId BlockId, INodeElectionSummary Summary, CancellationToken Token)
        {
            if (Summary.IsTrustable && Summary.IsAdoptable)
            {
                if (Summary.AdoptedEvidence.Data is null)
                    return false; // Nothing adopted.

                using (var Reader = new PacketReader(Summary.AdoptedEvidence.Data))
                {
                    try
                    {
                        var Id = new BlockId(Reader.ReadGuid());
                        if (Id != BlockId)
                        {
                            return false;
                        }

                        if (Reader.Read7BitEncodedInt() == 0)
                            return true;

                        if (!Block.TryImport(Reader, out var ReceivedBlock))
                            return false;

                        await m_Storage.PutAsync(BlockId, ReceivedBlock, Token);
                    }

                    catch (Exception e)
                    {
                        m_Logger.LogError(e, $"Failed to synchronize the block, {BlockId}.");
                        return false;
                    }

                    return true;
                }
            }

            else if (!Summary.IsTrustable)
            {
                m_Logger.LogInformation(
                    $"the received block ({BlockId}) is untrustable.");
            }

            else
            {
                m_Logger.LogInformation(
                    $"the received block ({BlockId}) has too many forks to select to synchronize.");
            }

            return false;
        }
    }
}
