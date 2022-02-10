using Backrole.Crypto;
using Glazer.Core.Helpers;
using Glazer.Core.Models;
using Glazer.Core.Models.Chains;
using Glazer.Core.Models.Interfaces;
using Glazer.Core.Notations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Nodes.Internals.Messages
{
    [NodeMessage("welcome")]
    internal class Welcome : IMessage
    {
        /// <summary>
        /// Chain information.
        /// </summary>
        public ChainInfo ChainInfo { get; set; }

        /// <summary>
        /// Assignment to sign.
        /// </summary>
        public byte[] Assignment { get; set; }

        /// <inheritdoc/>
        public void Encode(BinaryWriter Writer)
        {
            if (Assignment is null)
                Assignment = Rng.Make(512);

            Writer.Write(ChainInfo);
            Writer.WriteFrame(Assignment);
        }

        /// <inheritdoc/>
        public void Decode(BinaryReader Reader)
        {
            ChainInfo = Reader.ReadChainInfo(ChainInfo);
            Assignment = Reader.ReadFrame();
        }
    }

    [NodeMessage("welcome.reply")]
    internal class WelcomeReply : IMessage
    {
        /// <summary>
        /// Account Information.
        /// </summary>
        public Account Account { get; set; }

        /// <summary>
        /// Sign Value that generated for <see cref="Welcome.Assignment"/>.
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
