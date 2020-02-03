using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamLite.Packet.Data
{
    public struct PingRequest
    {
        public const int DATA_TYPE = 2;

        public DateTime PingTimeUtc { get; set; }

        public PingRequest(DateTime pingTimeUtc)
        {
            PingTimeUtc = pingTimeUtc;
        }

        public PingRequest(byte[] rawBytes)
        {
            this = JsonSerializer.GetStructFromBytes<PingRequest>(rawBytes);
        }

        public byte[] GetBytes()
        {
            return JsonSerializer.GetBytesFromStruct(this);
        }
    }
}
