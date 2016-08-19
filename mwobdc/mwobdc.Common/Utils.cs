using mwobdc.Common.Structs;
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

        public static void DumpObjectContents(string baseFileName, byte[] objectData, nameTableEntry[] nameTable)
        {
            var size = 0;
            var position = 0;

            DumpObjHeader(baseFileName, objectData, out size, out position);

            DumpHunkStart(baseFileName, objectData, ref size, ref position);

            var nextHunk = Hunk.HUNK_START;
            while (nextHunk != Hunk.HUNK_END)
            {
                nextHunk = PeekHunk(objectData, position); //get the next hunk type
                if (!ProcessHunk(nextHunk, baseFileName, objectData, nameTable, ref position))
                    break;
            } 

            DumpHunkData(baseFileName, objectData, position);

        }

        static bool ProcessHunk(Hunk nextHunk, string baseFileName, byte[] objectData, nameTableEntry[] nameTable, ref int position)
        {
            switch (nextHunk)
            {
                case Hunk.HUNK_GLOBAL_CODE:
                    position = DumpHunkGlobalCode(baseFileName, objectData, position, nameTable);
                    return true;

                default:
                    return false;
            }
        }

        static int DumpHunkGlobalCode(string baseFileName, byte[] objectData, int position, nameTableEntry[] nameTable)
        {
            //verify the data [Debug]
            var hunk = ReadObjCodeHunk(objectData, position);

            //we know that there are some values that are constant...
            System.Diagnostics.Debug.Assert(hunk.hunk_type == (Int16)Hunk.HUNK_GLOBAL_CODE);
            System.Diagnostics.Debug.Assert(hunk.sm_class == PowerPCConsts.XMC_PR || hunk.sm_class == PowerPCConsts.XMC_GL);
            if (hunk.sym_offset == 0x8000000)
            {
                System.Diagnostics.Debug.Assert(hunk.sym_decl_offset == 0);
            }

            var name = hunk.name_id - 1 >= 0 ? nameTable[hunk.name_id - 1].name : "none";

            using (var reader = new MemoryStream(objectData))
            {
                var fileName = baseFileName + $".DUMP__GlobalCode_{name}.txt";

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    var size = Marshal.SizeOf(typeof(ObjCodeHunk));
                    var buffer = new byte[size];
                    reader.Seek(position, SeekOrigin.Begin);
                    position += reader.Read(buffer, 0, size);
                    writer.Write(buffer);
                    writer.Close();
                }
            }
            return DumpGlobalCode(baseFileName, objectData, hunk.size, position, name);
        }

        /// <summary>
        /// Dumps the raw machine code that follows the hunk header
        /// </summary>
        static int DumpGlobalCode(string baseFileName, byte[] objectData, int size, int position, string name)
        {
            using (var reader = new MemoryStream(objectData))
            {
                var fileName = baseFileName + $".DUMP__GlobalCode_{name}_MC.txt";

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {                
                    var buffer = new byte[size];
                    reader.Seek(position, SeekOrigin.Begin);
                    position += reader.Read(buffer, 0, size);
                    writer.Write(buffer);
                    writer.Close();
                }
            }
            return position;
        }

        static void DumpHunkData(string baseFileName, byte[] objectData, int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                var fileName = baseFileName + ".DUMP__HunkData.txt";

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    reader.BaseStream.Position = position;
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        writer.Write(reader.ReadByte());
                    }
                }
            }
        }

        //dumps the hunk start data - which is 4 bytes, but hey - VALIDATION!!
        static void DumpHunkStart(string baseFileName, byte[] objectData, ref int size, ref int position)
        {
            using (var reader = new MemoryStream(objectData))
            {
                var fileName = baseFileName + ".DUMP__HunkStart.txt";

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    size = Marshal.SizeOf(typeof(ObjMiscHunk));
                    var buffer = new byte[size];
                    reader.Seek(position, SeekOrigin.Begin);
                    position += reader.Read(buffer, 0, size);
                    writer.Write(buffer);
                    writer.Close();
                }
            }
        }

        static void DumpObjHeader(string baseFileName, byte[] objectData, out int size, out int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                var fileName = baseFileName + ".DUMP__ObjHeader.txt";

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                position = 0;

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    size = Marshal.SizeOf(typeof(ObjHeader));
                    var buffer = new byte[size];
                    position = reader.Read(buffer, position, size);
                    writer.Write(buffer);
                    writer.Close();
                }
            }
        }

        static Hunk PeekHunk(byte[] objectData, int position)
        {
            var result = Hunk.HUNK_END;

            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                var hunkHeader =  Utils.Read<ObjPeekHunk>(reader);
                result =(Hunk)Utils.SwapInt16(hunkHeader.hunk_type);
            }

            return result;
        }

        static ObjCodeHunk ReadObjCodeHunk(byte[] objectData, int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                var result =  Utils.Read<ObjCodeHunk>(reader);
                //pre-process the fields
                result.hunk_type = Utils.SwapInt16(result.hunk_type);
                result.name_id = Utils.SwapInt32(result.name_id);
                result.size = Utils.SwapInt32(result.size);
                result.sym_offset = Utils.SwapInt32(result.sym_offset);
                result.sym_decl_offset = Utils.SwapInt32(result.sym_decl_offset);

                return result;
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
