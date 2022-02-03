using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IEncodableUnsealed
    {
        /// <summary>
        /// Serialize the instance to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        void EncodeUnsealed(BinaryWriter Writer, BlockOptions Options);

        /// <summary>
        /// Unserialize the instance from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        void DecodeUnsealed(BinaryReader Reader, BlockOptions Options);
    }
}
