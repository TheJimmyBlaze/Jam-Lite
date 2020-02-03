using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JamLite.Packet
{
    public static class StructMarshal
    {
        public static StructType GetStructFromBytes<StructType>(byte[] rawBytes)
        {
            object obj;
            int size = Marshal.SizeOf(typeof(StructType));

            IntPtr pointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(rawBytes, 0, pointer, size);
            obj = (StructType)Marshal.PtrToStructure(pointer, typeof(StructType));
            Marshal.FreeHGlobal(pointer);

            return (StructType)obj;
        }

        public static byte[] GetBytesFromStruct<StructType>(StructType inputStruct)
        {
            int size = Marshal.SizeOf(typeof(StructType));
            byte[] bytes = new byte[size];

            IntPtr pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(inputStruct, pointer, false);
            Marshal.Copy(pointer, bytes, 0, size);
            Marshal.FreeHGlobal(pointer);

            return bytes;
        }
    }
}
