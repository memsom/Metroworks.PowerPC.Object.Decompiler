using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace mwobdc.Common
{
    public static class Utils
    {
        static DateTime offset = new DateTime(1904, 01, 01, 00, 00, 00);

        public static object ToDateTime(uint value)
        {
            return offset.AddSeconds(value);
        }

        public static T Read<T>(BinaryReader data)
        {
            var buffer = new byte[Marshal.SizeOf(typeof(T))];
            int bytes = data.Read(buffer, 0, buffer.Length);

            T retval;
            var hdl = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                retval = (T)Marshal.PtrToStructure(hdl.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                hdl.Free();
            }
            return retval;
        }

        public static Int16 SwapInt16(Int16 v)
        {
            return (Int16)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }

        public static UInt16 SwapUInt16(UInt16 v)
        {
            return (UInt16)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }

        public static Int32 SwapInt32(Int32 v)
        {
            return (Int32)(((SwapInt16((Int16)v) & 0xffff) << 0x10) |
                          (SwapInt16((Int16)(v >> 0x10)) & 0xffff));
        }

        public static UInt32 SwapUInt32(UInt32 v)
        {
            return (UInt32)(((SwapUInt16((UInt16)v) & 0xffff) << 0x10) |
                           (SwapUInt16((UInt16)(v >> 0x10)) & 0xffff));

        }

        public static Int64 SwapInt64(Int64 v)
        {
            return (Int64)(((SwapInt32((Int32)v) & 0xffffffffL) << 0x20) |
                           (SwapInt32((Int32)(v >> 0x20)) & 0xffffffffL));
        }

        public static UInt64 SwapUInt64(UInt64 v)
        {
            return (UInt64)(((SwapUInt32((UInt32)v) & 0xffffffffL) << 0x20) |
                            (SwapUInt32((UInt32)(v >> 0x20)) & 0xffffffffL));

        }

        public static string GetString(BinaryReader file, Int32 fileOffset)
        {
            var result = new StringBuilder();
            var current = file.BaseStream.Position;
            try
            {
                file.BaseStream.Position = fileOffset;

                byte b = 0;
                do
                {
                    b = file.ReadByte();
                    if (b > 0)
                        result.Append((char)b);
                }
                while (b > 0);
            }
            finally
            {
                file.BaseStream.Position = current;
            }

            return result.ToString();
        }

        public static void DumpObject(string fileName, byte[] objectData)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var f = new BinaryWriter(File.Create(fileName)))
            {
                foreach (byte b in objectData)
                {
                    f.Write(b);
                }

                f.Close();
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CheckSum
    {
        [FieldOffset(0)]
        public byte lowByte;
        [FieldOffset(1)]
        public byte highByte;
        [FieldOffset(0)]
        public Int16 value;
    }
}
