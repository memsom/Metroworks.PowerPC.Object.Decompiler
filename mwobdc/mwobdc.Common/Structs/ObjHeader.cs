using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace mwobdc.Common.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjHeader
    {
        public Int32 magic_word;
        public Int16 version;
        public Int16 flags;
        public Int32 obj_size;
        public Int32 nametable_offset;
        public Int32 nametable_names;
        public Int32 symtable_offset;
        public Int32 symtable_size;
        public Int32 code_size;
        public Int32 udata_size;
        public Int32 idata_size;
        public Int32 toc;
        public Int32 old_def_version;
        public Int32 old_imp_version;
        public Int32 current_version;

        //this should be an array
        Int32 reserved_00;
        Int32 reserved_01;
        Int32 reserved_02;
        Int32 reserved_03;
        Int32 reserved_04;
        Int32 reserved_05;
        Int32 reserved_06;
        Int32 reserved_07;
        Int32 reserved_08;
        Int32 reserved_09;
        Int32 reserved_10;
        Int32 reserved_11;
        Int32 reserved_12;
    }

    public enum ObjectFlag
    {
        ObjIsSharedLib = 0x0001,
        ObjIsLib = 0x0002,
        ObjIsPascal = 0x0004,
        ObjIsWeak = 0x0008,
        ObjIsInitBefore = 0x0010
    }

    public class ObjHeaderEx
    {
        public ObjHeader Value;

        public bool IsSharedLib { get { return Value.FlagIsSet(ObjectFlag.ObjIsSharedLib); } }
        public bool IsLib { get { return Value.FlagIsSet(ObjectFlag.ObjIsLib); } }
        public bool IsPascal { get { return Value.FlagIsSet(ObjectFlag.ObjIsPascal); } }
        public bool IsWeak { get { return Value.FlagIsSet(ObjectFlag.ObjIsWeak); } }
        public bool IsInitBefore { get { return Value.FlagIsSet(ObjectFlag.ObjIsInitBefore); } }
    }

    public static class ObjHeaderHelper
    {
        public static string[] LiteralGetObjectNameTable(ObjHeader oh, byte[] oa)
        {
            var result = new List<string>();
            var buffer = new StringBuilder();
            for (int i = Utils.SwapInt32(oh.nametable_offset); i < oa.Length; i++)
            {
                buffer.Append((char)oa[i]);
            }

            var sa = buffer.ToString().Split('\0');
            foreach (var s in sa)
            {
                if (s.Length > 0)
                {
                    result.Add(s);
                }
            }

            return result.ToArray();
        }

        public static bool FlagIsSet(this ObjHeader oh,  ObjectFlag flag)
        {
            return oh.flags == (oh.flags | (Int16)flag);
        }

        /// <summary>
        /// Creates an array of nameTableEntry, which contain the name table info for this object
        /// </summary>
        public static nameTableEntry[] GetObjectNameTable(this ObjHeader oh, byte[] oa)
        {
            var buffer = new StringBuilder();
            var result = new List<nameTableEntry>();

            var ntoffset = Utils.SwapInt32(oh.nametable_offset);
            var ncount = Utils.SwapInt32(oh.nametable_names);
            var coffset = ntoffset; //set it to start of name table for the first entry

            for (var i = ntoffset; i < oa.Length; i++)
            {
                var eos = oa[i] == 0 && oa[i - 1] != 0;
                if (eos)
                {
                    result.Add(GetNameTableEntry(coffset, buffer));
                    buffer.Clear();
                    ncount--; //there should be the same number of entries as this count
                    coffset = i; //start of the entry
                }
                else
                {
                    buffer.Append((char)oa[i]);
                }
            }

            //ignore anything outside of the last entry as this is probably padding (??)

            return result.ToArray();
        }

        public static nameTableEntry GetNameTableEntry(int offsetValue, StringBuilder buffer)
        {
            var cs = new CheckSum();
            cs.highByte = (byte)buffer[0];
            cs.lowByte = (byte)buffer[1];
            buffer.Remove(0, 2);
            var nameValue = buffer.ToString();
            var csValue = MWHashUtils.CHash(nameValue);

            //System.Diagnostics.Debug.Assert(csValue == cs.value, $"Checksum failed.. expected {csValue.ToString("x")} got {cs.value.ToString("x")}");

            return new nameTableEntry { name = nameValue, check_sum = csValue, validated = csValue == cs.value, offset = offsetValue };

        }
    }
}
