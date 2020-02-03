using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamLite.Packet.Data
{
    public struct DebugMessageRequest
    {
        public const int DATA_TYPE = 5;

        public string Message { get; set; }

        public DebugMessageRequest(string message)
        {
            Message = message;
        }

        public DebugMessageRequest(byte[] rawBytes)
        {
            this = JsonSerializer.GetStructFromBytes<DebugMessageRequest>(rawBytes);
        }

        public byte[] GetBytes()
        {
            return JsonSerializer.GetBytesFromStruct(this);
        }
    }
}
