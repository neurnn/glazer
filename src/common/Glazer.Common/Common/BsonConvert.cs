using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Glazer.Common
{
    public static class BsonConvert
    {
        /// <summary>
        /// Deserialize the input bytes to <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Input"></param>
        /// <returns></returns>
        public static TValue Deserialize<TValue>(byte[] Input)
        {
            using var Reader = new PacketReader(Input);
            using var Bson = new BsonDataReader(Reader);

            return (TValue)JsonSerializer.CreateDefault().Deserialize(Bson, typeof(TValue));
        }

        /// <summary>
        /// Serialize the input Value to byte array.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static byte[] Serialize(object Value)
        {
            using var Writer = new PacketWriter();
            using (var Bson = new BsonDataWriter(Writer))
                JsonSerializer.CreateDefault().Serialize(Bson, Value);

            return Writer.ToByteArray();
        }
    }
}
