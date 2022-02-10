using Backrole.Crypto;
using Glazer.Core.Helpers;
using Glazer.Core.Models.Blocks;
using System;
using System.IO;

namespace Glazer.Core.Models.Chains
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
            Writer.Write(ChainInfo.ChainId.ToByteArray());
            Writer.Write(ChainInfo.GenesisTimeStamp);
            Writer.Write(ChainInfo.GenesisKey);
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
            Chain.ChainId = new Guid(Reader.ReadBytes(16));
            Chain.GenesisTimeStamp = Reader.ReadDateTime();
            Chain.GenesisKey = Reader.ReadSignPublicKey();

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
            ChainNode.Signature = Reader.ReadSignValue();

            return ChainNode;
        }
    }
}
