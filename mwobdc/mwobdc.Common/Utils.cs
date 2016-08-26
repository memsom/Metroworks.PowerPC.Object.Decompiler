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

            if (nextHunk == Hunk.HUNK_END)
            {
                DumpHunkEnd(baseFileName, objectData, ref size, ref position);
            }


            DumpHunkData(baseFileName, objectData, position);

        }

        static bool ProcessHunk(Hunk nextHunk, string baseFileName, byte[] objectData, nameTableEntry[] nameTable, ref int position)
        {
            switch (nextHunk)
            {
                case Hunk.HUNK_GLOBAL_CODE:
                case Hunk.HUNK_LOCAL_CODE:
                    position = DumpObjCodeHunk(nextHunk, baseFileName, objectData, position, nameTable);
                    return true;

                case Hunk.HUNK_GLOBAL_IDATA:
                case Hunk.HUNK_LOCAL_IDATA:
                case Hunk.HUNK_GLOBAL_UDATA:
                case Hunk.HUNK_LOCAL_UDATA:
                    position = DumpObjDataHunk(nextHunk, baseFileName, objectData, position, nameTable);
                    return true;

                case Hunk.HUNK_GLOBAL_ENTRY:
                case Hunk.HUNK_LOCAL_ENTRY:
                    position = DumpObjEntryHunk(nextHunk, baseFileName, objectData, position, nameTable);
                    return true;

                case Hunk.HUNK_XREF_16BIT:
                case Hunk.HUNK_XREF_16BIT_IL:
                case Hunk.HUNK_XREF_16BIT_REL:
                case Hunk.HUNK_XREF_24BIT:
                case Hunk.HUNK_XREF_32BIT:
                case Hunk.HUNK_XREF_32BIT_REL:
                    position = DumpObjXRefHunk(nextHunk, baseFileName, objectData, position, nameTable);
                    return true;

                //case Hunk.HUNK_GLOBAL_ENTRY:
                //case Hunk.HUNK_LOCAL_ENTRY:
                default:
                    return false;
            }
        }

        static int DumpObjXRefHunk(Hunk type, string baseFileName, byte[] objectData, int position, nameTableEntry[] nameTable)
        {
            System.Diagnostics.Debug.Assert(type == Hunk.HUNK_XREF_16BIT || type == Hunk.HUNK_XREF_16BIT_IL || type == Hunk.HUNK_XREF_16BIT_REL ||
                                            type == Hunk.HUNK_XREF_24BIT || type == Hunk.HUNK_XREF_32BIT || type == Hunk.HUNK_XREF_32BIT_REL);

            //verify the data [Debug]
            var hunk = ReadObjXRefHunk(objectData, position);

            var name = hunk.name_id - 1 >= 0 ? nameTable[hunk.name_id - 1].name : "none";

            var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}{type}_{name}.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

            DumpObjXRefHunkStruct(Path.ChangeExtension(fileName, ".txt"), hunk);

            using (var reader = new MemoryStream(objectData))
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    var size = Marshal.SizeOf(typeof(ObjXRefHunk));
                    var buffer = new byte[size];
                    reader.Seek(position, SeekOrigin.Begin);
                    position += reader.Read(buffer, 0, size);
                    writer.Write(buffer);
                    writer.Close();
                }
            }

            return position;
        }

        static void DumpObjXRefHunkStruct(string fileName, ObjXRefHunk hunk)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var writer = File.CreateText(fileName))
            {
                writer.WriteLine($"hunk_type: {hunk.hunk_type.ToHunk()}");
                writer.WriteLine($"sm_class: {hunk.sm_class}");
                writer.WriteLine($"unused: {hunk.unused}");
                writer.WriteLine($"name_id: {hunk.name_id}");
                writer.WriteLine($"offset: {hunk.offset.ToString("X")}");
                writer.Close();
            }
        }

        static int DumpObjEntryHunk(Hunk type, string baseFileName, byte[] objectData, int position, nameTableEntry[] nameTable)
        {
            System.Diagnostics.Debug.Assert(type == Hunk.HUNK_LOCAL_ENTRY || type == Hunk.HUNK_GLOBAL_ENTRY);

            //verify the data [Debug]
            var hunk = ReadObjEntryHunk(objectData, position);

            //we know that there are some values that are constant...
            if (hunk.sym_type_id == 0x8000000)
            {
                System.Diagnostics.Debug.Assert(hunk.sym_decl_offset == 0);
            }

            var name = hunk.name_id - 1 >= 0 ? nameTable[hunk.name_id - 1].name : "none";

            var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}{type}_{name}.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

            DumpObjEntryHunkStruct(Path.ChangeExtension(fileName, ".txt"), hunk);

            using (var reader = new MemoryStream(objectData))
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    var size = Marshal.SizeOf(typeof(ObjEntryHunk));
                    var buffer = new byte[size];
                    reader.Seek(position, SeekOrigin.Begin);
                    position += reader.Read(buffer, 0, size);
                    writer.Write(buffer);
                    writer.Close();
                }
            }

            return position;
        }

        static void DumpObjEntryHunkStruct(string fileName, ObjEntryHunk hunk)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var writer = File.CreateText(fileName))
            {
                writer.WriteLine($"hunk_type: {hunk.hunk_type.ToHunk()}");
                writer.WriteLine($"unused: {hunk.unused}");
                writer.WriteLine($"name_id: {hunk.name_id}");
                writer.WriteLine($"size: {hunk.offset}");
                writer.WriteLine($"sym_type_id: {hunk.sym_type_id.ToString("X")}");
                writer.WriteLine($"sym_decl_offset: {hunk.sym_decl_offset.ToString("X")}");
                writer.Close();
            }
        }

        static int DumpObjDataHunk(Hunk type, string baseFileName, byte[] objectData, int position, nameTableEntry[] nameTable)
        {
            System.Diagnostics.Debug.Assert(type == Hunk.HUNK_LOCAL_IDATA || type == Hunk.HUNK_GLOBAL_IDATA || type == Hunk.HUNK_LOCAL_UDATA || type == Hunk.HUNK_GLOBAL_UDATA);

            //verify the data [Debug]
            var hunk = ReadObjDataHunk(objectData, position);

            //we know that there are some values that are constant...
            System.Diagnostics.Debug.Assert(hunk.hunk_type == (Int16)type);
            System.Diagnostics.Debug.Assert(
                hunk.sm_class == PowerPCConsts.XMC_RO ||
                hunk.sm_class == PowerPCConsts.XMC_RW ||
                hunk.sm_class == PowerPCConsts.XMC_DS ||
                hunk.sm_class == PowerPCConsts.XMC_TC ||
                hunk.sm_class == PowerPCConsts.XMC_TD ||
                hunk.sm_class == PowerPCConsts.XMC_TC0 ||
                hunk.sm_class == PowerPCConsts.XMC_UNK0 |
                hunk.sm_class == PowerPCConsts.XMC_UNK1, $"expected a valid PowerPCConsts, got {hunk.sm_class}");

            if (hunk.sym_type_id == 0x8000000)
            {
                System.Diagnostics.Debug.Assert(hunk.sym_decl_offset == 0);
            }

            var name = hunk.name_id - 1 >= 0 ? nameTable[hunk.name_id - 1].name : "none";

            var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}{type}_{name}.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

            DumpObjDataHunkStruct(Path.ChangeExtension(fileName, ".txt"), hunk);

            using (var reader = new MemoryStream(objectData))
            {

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    var size = Marshal.SizeOf(typeof(ObjDataHunk));
                    var buffer = new byte[size];
                    reader.Seek(position, SeekOrigin.Begin);
                    position += reader.Read(buffer, 0, size);
                    writer.Write(buffer);
                    writer.Close();
                }
            }
            return DumpHunkContent($"{baseFileName}Dir{Path.DirectorySeparatorChar}{type}_{name}", objectData, hunk.size, position);
        }

        static void DumpObjDataHunkStruct(string fileName, ObjDataHunk hunk)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var writer = File.CreateText(fileName))
            {
                writer.WriteLine($"hunk_type: {hunk.hunk_type.ToHunk()}");
                writer.WriteLine($"sm_class: {hunk.sm_class}");
                writer.WriteLine($"x: {hunk.x}");
                writer.WriteLine($"name_id: {hunk.name_id}");
                writer.WriteLine($"size: {hunk.size}");
                writer.WriteLine($"sym_type_id: {hunk.sym_type_id.ToString("X")}");
                writer.WriteLine($"sym_decl_offset: {hunk.sym_decl_offset.ToString("X")}");
                writer.Close();
            }
        }

        static int DumpObjCodeHunk(Hunk type, string baseFileName, byte[] objectData, int position, nameTableEntry[] nameTable)
        {
            System.Diagnostics.Debug.Assert(type == Hunk.HUNK_GLOBAL_CODE || type == Hunk.HUNK_LOCAL_CODE);

            //verify the data [Debug]
            var hunk = ReadObjCodeHunk(objectData, position);

            //we know that there are some values that are constant...
            System.Diagnostics.Debug.Assert(hunk.hunk_type == (Int16)type);
            System.Diagnostics.Debug.Assert(hunk.sm_class == PowerPCConsts.XMC_PR || hunk.sm_class == PowerPCConsts.XMC_GL);
            if (hunk.sym_offset == 0x8000000)
            {
                System.Diagnostics.Debug.Assert(hunk.sym_decl_offset == 0);
            }

            var name = hunk.name_id - 1 >= 0 ? nameTable[hunk.name_id - 1].name : "none";

            var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}{type}_{name}.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

            DumpObjCodeHunkStruct(Path.ChangeExtension(fileName, ".txt"), hunk);

            using (var reader = new MemoryStream(objectData))
            {

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
            return DumpHunkContent($"{baseFileName}Dir{Path.DirectorySeparatorChar}{type}_{name}", objectData, hunk.size, position);
        }

        static void DumpObjCodeHunkStruct(string fileName, ObjCodeHunk hunk)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (var writer = File.CreateText(fileName))
            {
                writer.WriteLine($"hunk_type: {hunk.hunk_type.ToHunk()}");
                writer.WriteLine($"sm_class: {hunk.sm_class}");
                writer.WriteLine($"x: {hunk.x}");
                writer.WriteLine($"name_id: {hunk.name_id}");
                writer.WriteLine($"size: {hunk.size}");
                writer.WriteLine($"sym_offset: {hunk.sym_offset.ToString("X")}");
                writer.WriteLine($"sym_decl_offset: {hunk.sym_decl_offset.ToString("X")}");
                writer.Close();
            }
        }

        /// <summary>
        /// Dumps the raw machine code that follows the hunk header
        /// </summary>
        static int DumpHunkContent(string baseFileName, byte[] objectData, int size, int position, bool dumpHex = true)
        {
            using (var reader = new MemoryStream(objectData))
            {
                var fileName = $"{baseFileName}_Content.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                if (dumpHex)
                {
                    DumpHunkContentAsHex(fileName, objectData, size, position);
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

        /// <summary>
        /// Threw this in to make looking at the hex simpler.
        /// </summary>
        static void DumpHunkContentAsHex(string fileName, byte[] objectData, int size, int position)
        {
            using (var reader = new MemoryStream(objectData))
            {
                fileName = Path.ChangeExtension(fileName, ".hex");
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var writer = new BinaryWriter(File.Create(fileName)))
                {
                    var buffer = new byte[size];
                    reader.Seek(position, SeekOrigin.Begin);
                    position += reader.Read(buffer, 0, size);

                    foreach (var b in buffer)
                        writer.Write($"{b.ToString("X2")} ".ToCharArray());

                    writer.Close();
                }
            }
        }

        static void DumpHunkData(string baseFileName, byte[] objectData, int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}HunkData.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

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
                var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}HunkStart.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

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

        //dumps the hunk start data - which is 4 bytes, but hey - VALIDATION!!
        static void DumpHunkEnd(string baseFileName, byte[] objectData, ref int size, ref int position)
        {
            using (var reader = new MemoryStream(objectData))
            {
                var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}HunkEnd.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

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
            var fileName = $"{baseFileName}Dir{Path.DirectorySeparatorChar}ObjHeader.bin".Replace('<', '+').Replace('>', '+').Replace('/', '+');

            var obj = ReadObjHeader(objectData);

            DumpObjHeaderStruct(Path.ChangeExtension(fileName, ".txt"), obj);

            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
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

        static void DumpObjHeaderStruct(string fileName, ObjHeader obj)
        {
            if (File.Exists(fileName))
            { 
                File.Delete(fileName);
            }

            using (var writer = File.CreateText(fileName))
            {

                writer.WriteLine($"magic_word: {obj.magic_word.ToString("X")}");
                writer.WriteLine($"version: {obj.version.ToString("X")}");
                writer.WriteLine($"flags: {obj.flags}");
                var ohx = new ObjHeaderEx { Value = obj };
                writer.WriteLine($"\tIsSharedLib? {ohx.IsSharedLib}");
                writer.WriteLine($"\tIsLib? {ohx.IsLib}");
                writer.WriteLine($"\tIsPascal? {ohx.IsPascal}");
                writer.WriteLine($"\tIsWeak? {ohx.IsWeak}");
                writer.WriteLine($"\tIsInitBefore? {ohx.IsInitBefore}");
                writer.WriteLine($"obj_size: {obj.obj_size}");
                writer.WriteLine($"nametable_offset: {obj.nametable_offset}");
                writer.WriteLine($"nametable_names: {obj.nametable_names}");
                writer.WriteLine($"symtable_offset: {obj.symtable_offset}");
                writer.WriteLine($"symtable_size: {obj.symtable_size}");
                writer.WriteLine($"code_size: {obj.code_size}");
                writer.WriteLine($"udata_size: {obj.udata_size}");
                writer.WriteLine($"idata_size: {obj.idata_size}");
                writer.WriteLine($"toc: {obj.toc}");
                writer.WriteLine($"old_def_version: {obj.old_def_version}");
                writer.WriteLine($"old_imp_version: {obj.old_imp_version}");
                writer.WriteLine($"current_version: {obj.current_version }");
                writer.Close();
            }
        }

        static Hunk PeekHunk(byte[] objectData, int position)
        {
            var result = Hunk.HUNK_END;

            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                var hunkHeader = Utils.Read<ObjPeekHunk>(reader);
                result = (Hunk)Utils.SwapInt16(hunkHeader.hunk_type);
            }

            return result;
        }

        static ObjXRefHunk ReadObjXRefHunk(byte[] objectData, int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                var result = Utils.Read<ObjXRefHunk>(reader);
                //pre-process the fields
                result.hunk_type = Utils.SwapInt16(result.hunk_type);
                result.name_id = Utils.SwapInt32(result.name_id);
                result.offset = Utils.SwapInt32(result.offset);

                return result;
            }
        }

        static ObjCodeHunk ReadObjCodeHunk(byte[] objectData, int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                var result = Utils.Read<ObjCodeHunk>(reader);
                //pre-process the fields
                result.hunk_type = Utils.SwapInt16(result.hunk_type);
                result.name_id = Utils.SwapInt32(result.name_id);
                result.size = Utils.SwapInt32(result.size);
                result.sym_offset = Utils.SwapInt32(result.sym_offset);
                result.sym_decl_offset = Utils.SwapInt32(result.sym_decl_offset);

                return result;
            }
        }

        static ObjDataHunk ReadObjDataHunk(byte[] objectData, int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                var result = Utils.Read<ObjDataHunk>(reader);
                //pre-process the fields
                result.hunk_type = Utils.SwapInt16(result.hunk_type);
                result.name_id = Utils.SwapInt32(result.name_id);
                result.size = Utils.SwapInt32(result.size);
                result.sym_type_id = Utils.SwapInt32(result.sym_type_id);
                result.sym_decl_offset = Utils.SwapInt32(result.sym_decl_offset);

                return result;
            }
        }

        static ObjEntryHunk ReadObjEntryHunk(byte[] objectData, int position)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                var result = Utils.Read<ObjEntryHunk>(reader);
                //pre-process the fields
                result.hunk_type = Utils.SwapInt16(result.hunk_type);
                result.name_id = Utils.SwapInt32(result.name_id);
                result.offset = Utils.SwapInt32(result.offset);
                result.sym_type_id = Utils.SwapInt32(result.sym_type_id);
                result.sym_decl_offset = Utils.SwapInt32(result.sym_decl_offset);

                return result;
            }
        }

        static ObjHeader ReadObjHeader(byte[] objectData)
        {
            using (var reader = new BinaryReader(new MemoryStream(objectData)))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                var result = Utils.Read<ObjHeader>(reader);
                //pre-process the fields
                result.magic_word = Utils.SwapInt32(result.magic_word);
                result.version = Utils.SwapInt16(result.version);
                result.flags = Utils.SwapInt16(result.flags);
                result.obj_size = Utils.SwapInt32(result.obj_size);
                result.nametable_offset = Utils.SwapInt32(result.nametable_offset);
                result.nametable_names = Utils.SwapInt32(result.nametable_names);
                result.symtable_offset = Utils.SwapInt32(result.symtable_offset);
                result.symtable_size = Utils.SwapInt32(result.symtable_size);
                result.code_size = Utils.SwapInt32(result.code_size);
                result.udata_size = Utils.SwapInt32(result.udata_size);
                result.idata_size = Utils.SwapInt32(result.idata_size);
                result.toc = Utils.SwapInt32(result.toc);
                result.old_def_version = Utils.SwapInt32(result.old_def_version);
                result.old_imp_version = Utils.SwapInt32(result.old_imp_version);
                result.current_version = Utils.SwapInt32(result.current_version);

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
