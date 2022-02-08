using Backrole.Crypto;
using Glazer.Nodes.Helpers;
using Glazer.Nodes.Models.Blocks;
using Glazer.Nodes.Models.Contracts;
using System.IO;

namespace Glazer.Nodes.Models.Chains
{
    public static class ChainInfoHelpers
    {
        /// <summary>
        /// Write the <see cref="ChainInfo"/> to <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="ChainInfo"></param>
        public static void Write(this BinaryWriter Writer, ChainInfo ChainInfo)
        {
            Writer.Write(ChainInfo.Version);
            Writer.Write(ChainInfo.ChainId);
            Writer.Write(ChainInfo.GenesisTimeStamp);
            Writer.Write(ChainInfo.GenesisKey);
            Writer.Write(ChainInfo.LastBlockIndex);
            Writer.Write(ChainInfo.LastBlockHash);
        }

        /// <summary>
        /// Read the <see cref="ChainInfo"/> from <see cref="BinaryReader"/>
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Recycle"></param>
        public static ChainInfo ReadChainInfo(this BinaryReader Reader, ChainInfo Recycle = null)
        {
            var Chain = Recycle ?? new ChainInfo();

            Chain.Version = Reader.ReadUInt32();
            Chain.ChainId = Reader.ReadString();
            Chain.GenesisTimeStamp = Reader.ReadDateTime();
            Chain.GenesisKey = Reader.ReadSignPublicKey();
            Chain.LastBlockIndex = Reader.ReadBlockIndex();
            Chain.LastBlockHash = Reader.ReadHashValue();

            return Chain;
        }

        /// <summary>
        /// Write the <see cref="ChainNodeInfo"/> to <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="NodeInfo"></param>
        public static void Write(this BinaryWriter Writer, ChainNodeInfo NodeInfo, bool WithSign = true)
        {
            Writer.Write(NodeInfo.Chain);
            Writer.Write(NodeInfo.TimeStamp);
            Writer.Write(NodeInfo.Account);
            Writer.Write((byte) NodeInfo.Feature);
            Writer.Write(WithSign ? NodeInfo.Signature : SignValue.Empty);
        }

        /// <summary>
        /// Read the <see cref="ChainNodeInfo"/> from <see cref="BinaryReader"/>
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="Recycle"></param>
        public static ChainNodeInfo ReadChainNodeInfo(this BinaryReader Reader, ChainNodeInfo Recycle = null)
        {
            var ChainNode = Recycle ?? new ChainNodeInfo();

            if (ChainNode.Chain is null)
                ChainNode.Chain = Reader.ReadChainInfo();

            else
                ChainNode.Chain = Reader.ReadChainInfo(ChainNode.Chain);

            ChainNode.TimeStamp = Reader.ReadDateTime();
            ChainNode.Account = Reader.ReadAccount();
            ChainNode.Feature = (NodeFeatureType)Reader.ReadByte();
            ChainNode.Signature = Reader.ReadSignValue();

            return ChainNode;
        }
    }
}
