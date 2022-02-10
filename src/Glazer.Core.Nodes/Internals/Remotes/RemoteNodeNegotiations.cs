using Backrole.Core.Hosting;
using Backrole.Core.Abstractions;
using Glazer.Core.Nodes.Internals.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;
using Glazer.Core.Models.Chains;
using Glazer.Core.Services;
using Glazer.Core.Models.Blocks;
using Backrole.Crypto;
using System.Net;
using Glazer.Core.Models;

namespace Glazer.Core.Nodes.Internals.Remotes
{
    internal class RemoteNodeNegotiations
    {
        private RemoteNode m_Node;
        private RemoteNodeSender m_Sender;

        private bool m_RequestArrived = false;

        /// <summary>
        /// Initialize a new <see cref="RemoteNodeNegotiations"/> instance.
        /// </summary>
        /// <param name="Sender"></param>
        public RemoteNodeNegotiations(RemoteNode Node)
        {
            m_Node = Node; 
            m_Sender = Node.GetRequiredService<RemoteNodeSender>();
        }

        /// <summary>
        /// Negotiate with the remote host.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task NegotiateAsync()
        {
            var Success = false;
            var Token = m_Node.GetRequiredService<CancellationToken>();
            try
            {
                var LocalNode = m_Node.GetRequiredService<ILocalNode>();
                var Settings = LocalNode.GetRequiredService<IOptions<LocalNodeSettings>>().Value;

                var Blocks = LocalNode.GetRequiredService<IBlockRepository>();
                var Accounts = LocalNode.GetRequiredService<IAccountManager>();

                var Genesis = await Blocks.GetAsync(BlockIndex.Genesis, Token);
                if (Genesis is null || Accounts is null) /* --> if no genesis performed, no negotiate. */
                    return;

                var Request = new Welcome
                {
                    ChainInfo = new ChainInfo
                    {
                        Version = 0,
                        ChainId = Settings.ChainId,
                        GenesisKey = Genesis.Header.Producer.PublicKey,
                        GenesisTimeStamp = Genesis.Header.TimeStamp
                    }
                };

                /* If the reply is not WelcomeReply, */
                if ((await m_Sender.Request(Request, Token)) is not WelcomeReply Reply)
                    return;

                /* Check the account exists or not. */
                var Seal = new SignSealValue(Reply.SignValue, Reply.Account.PublicKey);
                var Status = await Accounts.VerifyAsync(Reply.Account.LoginName, Seal, Request.Assignment, Token);
                if (Status != HttpStatusCode.OK) return;

                /* Okay, good. */
                m_Node.SetAccount(Reply.Account);
                Success = true;
            }
            finally
            {
                if (!Success) /* --> Self Suicide if not successful. */
                    m_Node.GetRequiredService<CancellationTokenSource>().Cancel();
            }
        }

        /// <summary>
        /// Called to handle the negotiation `Welcome` message.
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public async Task<WelcomeReply> OnNegotiation(Welcome Request)
        {
            lock(this)
            {
                if (m_RequestArrived) /* --> Malicious request. */
                {
                    m_Node.GetRequiredService<CancellationTokenSource>().Cancel();
                    return null;
                }

                m_RequestArrived = true;
            }

            var Token = m_Node.GetRequiredService<CancellationToken>();

            var LocalNode = m_Node.GetRequiredService<ILocalNode>();
            var Settings = LocalNode.GetRequiredService<IOptions<LocalNodeSettings>>().Value;

            var Blocks = LocalNode.GetRequiredService<IBlockRepository>();
            var Genesis = await Blocks.GetAsync(BlockIndex.Genesis, Token);
            if (Genesis is null) /* --> if no genesis performed, no negotiate. */
                return null;

            if (Request.ChainInfo.Version != 0)
                return null; // --> Version Mismatch.

            if (Request.ChainInfo.ChainId != Settings.ChainId)
                return null;

            if (Request.ChainInfo.GenesisKey != Genesis.Header.Producer.PublicKey)
                return null;

            if (Request.ChainInfo.GenesisTimeStamp != Genesis.Header.TimeStamp)
                return null;

            return new WelcomeReply /* --> Reply about the local node. */
            {
                Account = new Account(Settings.Login, Settings.KeyPair.PublicKey),
                SignValue = Settings.KeyPair.Sign(Request.Assignment)
            };
        }
    }
}
