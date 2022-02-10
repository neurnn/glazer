using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Models.Interfaces
{
    /// <summary>
    /// Binary Message.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Encode the message to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        void Encode(BinaryWriter Writer);

        /// <summary>
        /// Decode the message from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        void Decode(BinaryReader Reader);
    }
}
