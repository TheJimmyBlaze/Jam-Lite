using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamLite.Packet
{
    public struct JamPacketHeader
    {
        public int DataType;
        public int DataLength;

        public JamPacketHeader(byte[] rawBytes)
        {
            this = StructMarshal.GetStructFromBytes<JamPacketHeader>(rawBytes);
        }

        public byte[] GetBytes()
        {
            return StructMarshal.GetBytesFromStruct(this);
        }
    }
}
