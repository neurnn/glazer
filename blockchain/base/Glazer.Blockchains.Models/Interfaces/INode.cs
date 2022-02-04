using Glazer.Blockchains.Models.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    /// <summary>
    /// Node interface.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Mode of the node.
        /// </summary>
        NodeMode Mode { get; }

        /// <summary>
        /// Login of the node itself.
        /// </summary>
        string Login { get; }

        /// <summary>
        /// Options of the node.
        /// </summary>
        NodeOptions Options { get; }

        /// <summary>
        /// Genesis Block information.
        /// (Null if genesis did not be performed)
        /// </summary>
        Block GenesisBlock { get; }

        /// <summary>
        /// Peer Information if the node is remote node.
        /// </summary>
        PeerInfo PeerInfo { get; }

        /// <summary>
        /// Block Repository.
        /// </summary>
        IBlockRepository Blocks { get; }

        /// <summary>
        /// Code Repository.
        /// </summary>
        IRecordRepository CodeRepository { get; }

        /// <summary>
        /// Record Repository.
        /// </summary>
        IRecordRepository GetRecordRepository(Guid CodeId);

        /// <summary>
        /// Execute transactions on the node with cancellation token.
        /// </summary>
        /// <param name="Transactions"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<ExecutionResult> ExecuteAsync(IEnumerable<Transaction> Transactions, CancellationToken Token = default);
    }
}
