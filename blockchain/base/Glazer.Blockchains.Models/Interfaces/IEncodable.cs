using System.IO;

namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IEncodable
    {
        /// <summary>
        /// Encode the instance to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        void Encode(BinaryWriter Writer, BlockOptions Options);

        /// <summary>
        /// Decode the instance from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        void Decode(BinaryReader Reader, BlockOptions Options);
    }
}
