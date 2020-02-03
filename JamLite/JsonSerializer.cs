using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamLite
{
    public static class JsonSerializer
    {
        public static StructType GetStructFromBytes<StructType>(byte[] rawBytes)
        {
            string structText = Encoding.ASCII.GetString(rawBytes);
            return JsonConvert.DeserializeObject<StructType>(structText);
        }

        public static byte[] GetBytesFromStruct<StructType>(StructType structType)
        {
            JsonSerializerSettings ignoreLoopback = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            string structText = JsonConvert.SerializeObject(structType, Formatting.None, ignoreLoopback);
            return Encoding.ASCII.GetBytes(structText);
        }
    }
}
