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
        public UInt32 moddate; //this seems to be from a different offset in BeOS.. might actually be unused. Compiling under Haiku with cross compiler gave a value of 0.
        public Int32 filename;
        public Int32 fullpathname; //this seems to be set to 0 in BeOS - the filename is all that is stored
        public Int32 objectstart;
        public Int32 objectsize;
    }

    public struct nameTableEntry
    {
        public string name; //the name in the name table
        public Int16 check_sum; //the check_sum value calculated using MWHashUtile.CHash(..)
        public bool validated; //was the check_sum validated for this entry (i.e. when we read this record, did we re-compute it and compare this value to it)
        public Int32 offset; //offset with in the object
    }

    public class LibFileEx
    {
        public LibFile LibFile;
        public string FileName { get; set; }
        public string FullPathName { get; set; } //BeOS doesn't seem to use this
        public byte[] Object { get; set; }
        //TODO: make this not inline
        public ObjHeaderEx ObjectHeader
        {
            get
            {
                return new ObjHeaderEx { Value = GetObjectHeader() };
            }
        }

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
            get { return (ObjectHeader.Value.GetObjectNameTable(Object)); }
        }        
    }

    public static class LibFileHelper
    {
        public static string GetFullPathName(this LibFile libFile, BinaryReader file)
        {
            if (libFile.fullpathname > 0)
                return Utils.GetString(file, Utils.SwapInt32(libFile.fullpathname));
            else
                return string.Empty;
        }

        public static string GetFileName(this LibFile libFile, BinaryReader file)
        {
            if (libFile.filename > 0)
                return Utils.GetString(file, Utils.SwapInt32(libFile.filename));
            else
                return string.Empty;
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
