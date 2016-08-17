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

        [Obsolete]
        public static string[] GetObjectNameTable(ObjHeader oh, byte[] oa)
        {
            var buffer = new StringBuilder();
            var result = new List<string>();

            var ntoffset = Utils.SwapInt32(oh.nametable_offset);
            var ncount = Utils.SwapInt32(oh.nametable_names);

            for (var i = ntoffset; i < oa.Length; i++)
            {
                var eos = oa[i] == 0 && oa[i - 1] != 0;
                if (eos)
                {
                    result.Add(buffer.ToString());
                    buffer.Clear();
                    ncount--;
                }
                else
                {
                    buffer.Append((char)oa[i]);
                }
            }

            result.Add(buffer.ToString());

            return result.ToArray();
        }
    }
}
