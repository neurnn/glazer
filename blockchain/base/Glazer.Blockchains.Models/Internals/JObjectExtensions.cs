using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Blockchains.Models.Internals
{
    public static class JObjectExtensions
    {
        private static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        /// Encode the JObject to BSON bytes.
        /// </summary>
        /// <param name="This"></param>
        /// <returns></returns>
        public static byte[] EncodeAsBson(this JObject This)
        {
            if (This != null)
            {
                using var Stream = new MemoryStream();
                using var BsonWriter = new BsonDataWriter(Stream);

                JsonSerializer.CreateDefault().Serialize(BsonWriter, This);
                BsonWriter.Flush();

                return Stream.ToArray();
            }

            return EMPTY_BYTES;
        }

        /// <summary>
        /// Decode the JObject from BSON bytes.
        /// </summary>
        /// <param name="Bson"></param>
        /// <returns></returns>
        public static JObject DecodeAsBson(this byte[] Bson)
        {
            if (Bson != null && Bson.Length > 0)
            {
                using var Stream = new MemoryStream(Bson);
                using var BsonReader = new BsonDataReader(Stream);

                return JsonSerializer.CreateDefault().Deserialize<JObject>(BsonReader);
            }

            return null;
        }
    }
}
