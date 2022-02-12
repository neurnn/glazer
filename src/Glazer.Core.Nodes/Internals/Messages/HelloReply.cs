using Backrole.Crypto;
using Glazer.Core.Helpers;
using Glazer.Core.Models;
using Glazer.Core.Models.Chains;
using Glazer.Core.Models.Interfaces;
using Glazer.Core.Notations;
using System.IO;

namespace Glazer.Core.Nodes.Internals.Messages
{
    [NodeMessage("hello.reply")]
    internal class HelloReply : IMessage
    {
        /// <summary>
        /// Account Information.
        /// </summary>
        public Account Account { get; set; }

        /// <summary>
        /// Sign Value that generated for <see cref="Hello.Assignment"/>.
        /// </summary>
        public SignValue SignValue { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            Writer.Write(Account);
            Writer.Write(SignValue);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            Account = Reader.ReadAccount();
            SignValue = Reader.ReadSignValue();
        }

    }
}
