using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamLite.Packet.Data
{
    public struct PingResponse
    {
        public const int DATA_TYPE = 1;

        public DateTime PingTimeUtc { get; set; }
        public DateTime PongTimeUtc { get; set; }

        public PingResponse(DateTime pingTimeUtc, DateTime pongTimeUtc)
        {
            PingTimeUtc = pingTimeUtc;
            PongTimeUtc = pongTimeUtc;
        }

        public PingResponse(byte[] rawBytes)
        {
            this = JsonSerializer.GetStructFromBytes<PingResponse>(rawBytes);
        }

        public byte[] GetBytes()
        {
            return JsonSerializer.GetBytesFromStruct(this);
        }
    }
}
