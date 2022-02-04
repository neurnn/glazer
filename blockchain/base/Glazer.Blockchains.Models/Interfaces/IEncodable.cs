using Glazer.Blockchains.Models.Results;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IEncodable
    {
        /// <summary>
        /// Encode the instance to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        void Encode(BinaryWriter Writer, NodeOptions Options);

        /// <summary>
        /// Decode the instance from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        void Decode(BinaryReader Reader, NodeOptions Options);
    }

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
        /// Record Repository.
        /// </summary>
        IRecordRepository Records { get; }

        /// <summary>
        /// Execute transactions on the node with cancellation token.
        /// </summary>
        /// <param name="Transactions"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<ExecutionResult> ExecuteAsync(IEnumerable<Transaction> Transactions, CancellationToken Token = default);

        
    }
}
