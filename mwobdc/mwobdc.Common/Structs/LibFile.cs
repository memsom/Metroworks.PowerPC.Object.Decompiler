using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace mwobdc.Common.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LibFile
    {
        public UInt32 moddate;
        public Int32 filename;
        public Int32 fullpathname;
        public Int32 objectstart;
        public Int32 objectsize;
    }

    public struct nameTableEntry
    {
        public string name;
        public Int16 check_sum;
        public bool validated;
    }

    public class LibFileEx
    {
        public LibFile LibFile;
        public string FileName { get; set; }
        public string FullPathName { get; set; }
        public byte[] Object { get; set; }
        public ObjHeader ObjectHeader { get { return GetObjectHeader(); } }

        ObjHeader GetObjectHeader()
        {
            using (var ms = new MemoryStream(Object))
            {
                using (var file = new BinaryReader(ms))
                {
                    return Utils.Read<ObjHeader>(file);
                }
            }
        }

        public nameTableEntry[] ObjectNameTable
        {
            get { return GetObjectNameTable(ObjectHeader, Object); }
        }

        nameTableEntry[] GetObjectNameTable(ObjHeader oh, byte[] oa)
        {
            var buffer = new StringBuilder();
            var result = new List<nameTableEntry>();

            var ntoffset = Utils.SwapInt32(oh.nametable_offset);
            var ncount = Utils.SwapInt32(oh.nametable_names);

            for (var i = ntoffset; i < oa.Length; i++)
            {
                var eos = oa[i] == 0 && oa[i - 1] != 0;
                if (eos)
                {
                    result.Add(GetNameTableEntry(buffer));
                    buffer.Clear();
                    ncount--;
                }
                else
                {
                    buffer.Append((char)oa[i]);
                }
            }

            //if (buffer.Length > 0)
            //    result.Add(GetNameTableEntry(buffer));

            return result.ToArray();
        }

        nameTableEntry GetNameTableEntry(StringBuilder buffer)
        {
            var cs = new CheckSum();
            cs.highByte = (byte)buffer[0];
            cs.lowByte = (byte)buffer[1];
            buffer.Remove(0, 2);
            var nameValue = buffer.ToString();
            var csValue = MWHashUtils.CHash(nameValue);

            //System.Diagnostics.Debug.Assert(csValue == cs.value, $"Checksum failed.. expected {csValue.ToString("x")} got {cs.value.ToString("x")}");

            return new nameTableEntry { name = nameValue, check_sum = csValue, validated = csValue == cs.value };

        }
    }

    public static class LibFileHelper
    {
        public static string GetFullPathName(this LibFile libFile, BinaryReader file)
        {
            return Utils.GetString(file, Utils.SwapInt32(libFile.fullpathname));
        }

        public static string GetFileName(this LibFile libFile, BinaryReader file)
        {
            return Utils.GetString(file, Utils.SwapInt32(libFile.filename));
        }

        public static byte[] GetObject(this LibFile libFile, BinaryReader file)
        {
            byte[] result = null;
            var current = file.BaseStream.Position;
            try
            {
                file.BaseStream.Position = Utils.SwapInt32(libFile.objectstart);
                var size = Utils.SwapInt32(libFile.objectsize);
                result = file.ReadBytes(size);

                System.Diagnostics.Debug.Assert(result.Length == size);
            }
            finally
            {
                file.BaseStream.Position = current;
            }
            return result;
        }
    }
}
