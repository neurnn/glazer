using Glazer.Core.Models.Interfaces;
using Glazer.Core.Notations;
using System.IO;

namespace Glazer.Core.Nodes.Internals.Messages
{
    [NodeMessage("heartbeat.reply")]
    internal class HeartbeatReply : IMessage
    {
        /// <summary>
        /// Count of the heartbeat received node.
        /// </summary>
        public int DeliveredNodes { get; set; } = 0;

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write7BitEncodedInt(DeliveredNodes);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            DeliveredNodes = Reader.Read7BitEncodedInt();
        }
    }
}
