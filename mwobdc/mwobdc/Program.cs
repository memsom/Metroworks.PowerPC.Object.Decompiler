using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;

namespace mwobdc
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LibHeader
    {
        public Int32 magicword;
        public Int32 magicproc;
        public Int32 magicflags;
        public Int32 version;
        public Int32 code_size;
        public Int32 data_size;
        public Int32 nobjectfiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct LibFile
    {
        public UInt32 moddate;
        public Int32 filename;
        public Int32 fullpathname;
        public Int32 objectstart;
        public Int32 objectsize;
    }

    class LibFileEx
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

        public string[] ObjectNameTable { get; set; }
    }

    struct ObjHeader
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

    public static class Utils
    {
        public static T Read<T>(BinaryReader data)
        {
            var buffer = new byte[Marshal.SizeOf(typeof(T))];
            int bytes = data.Read(buffer, 0, buffer.Length);

            T retval;
            GCHandle hdl = GCHandle.Alloc(buffer, GCHandleType.Pinned);
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

        public static short SwapInt16(short v)
        {

            return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));

        }

        public static ushort SwapUInt16(ushort v)
        {

            return (ushort)(((v & 0xff) << 8) | ((v >> 8) & 0xff));

        }

        public static int SwapInt32(int v)

        {

            return (int)(((SwapInt16((short)v) & 0xffff) << 0x10) |

                          (SwapInt16((short)(v >> 0x10)) & 0xffff));

        }

        public static uint SwapUInt32(uint v)
        {

            return (uint)(((SwapUInt16((ushort)v) & 0xffff) << 0x10) |

                           (SwapUInt16((ushort)(v >> 0x10)) & 0xffff));

        }

        public static long SwapInt64(long v)
        {

            return (long)(((SwapInt32((int)v) & 0xffffffffL) << 0x20) |

                           (SwapInt32((int)(v >> 0x20)) & 0xffffffffL));

        }

        public static ulong SwapUInt64(ulong v)
        {

            return (ulong)(((SwapUInt32((uint)v) & 0xffffffffL) << 0x20) |

                            (SwapUInt32((uint)(v >> 0x20)) & 0xffffffffL));

        }
    }

    /// <summary>
    /// 
    /// </summary> 
    public static class MWHashUtils
    {
        public const Int16 NAMEHASH = 2048;

        static int swap_int16(int val)
        {
            return (val << 8) | ((val >> 8) & 0xFF);
        }

        //Converted from C - some nasty legacy MetroWerks code...
        //
        //SInt16 CHash(char *string)
        //{
        //  SInt16 i, hashval;
        //   unsigned char u;
        //   if ((hashval = strlen(string) & 0x0FF) != 0)
        //   {
        //       for(i = hashval, u = 0; i > 0; i--)
        //       {
        //u=(u>>3)|(u<<5);
        //           u += *string++;
        //       }
        //     hashval = (hashval << 8) | u;
        //   }     
        //   return (hashval &(NAMEHASH -1));
        //}

        /// <summary>
        /// Creates a MW Hash - this is as close to the original as there is a point in making it
        /// </summary>
        public static Int16 CHash(char[] cstring)
        {
            int i, hashval;
            byte u, u2; //as pointers are complete overkill in C#, we instead use an array indexor

            hashval = cstring.Length;

            if ((hashval & 0x0FF) != 0)
            {
                for (i = hashval, u = 0, u2 = 0; i > 0; i--)
                {
                    var us1 = (byte)(u >> 3); //broke this in to lines as it made debugging simpler
                    var us2 = (byte)(u << 5);
                    u = (byte)(us1 | us2);
                    u += (byte)(cstring[u2++]);
                }
                hashval = ((hashval << 8) | u);
            }

            return (Int16)(hashval & (NAMEHASH - 1));
        }

        //wrapper - no use, just to simplify
        public static Int16 CHash(string cstring)
        {
            return CHash(cstring.ToArray());
        }
    }

    class Program
    {
        static DateTime offset = new DateTime(1904, 01, 01, 00, 00, 00);

        static Int32 C(Int32 value)
        {
            return Utils.SwapInt32(value);
        }

        static UInt32 UC(UInt32 value)
        {
            return Utils.SwapUInt32(value);
        }

        static object ToDateTime(uint value)
        {
            return offset.AddSeconds(value);
        }

        static void Main(string[] args)
        {
            using (var fs = new FileStream("mslstdrt.o", FileMode.Open))
            {
                using (var file = new BinaryReader(fs))
                {
                    var mwobLibHeader = Utils.Read<LibHeader>(file);

                    Console.WriteLine($"MWOB\r\n\tmagicword {C(mwobLibHeader.magicword).ToString("X")}\r\n\tmagicproc {C(mwobLibHeader.magicproc).ToString("X")}\r\n\tmagicflags {C(mwobLibHeader.magicflags)}\r\n\tversion {C(mwobLibHeader.version)}\r\n\tcode_size {C(mwobLibHeader.code_size)}\r\n\tdata_size {C(mwobLibHeader.data_size)}\r\n\tnobjectfiles {C(mwobLibHeader.nobjectfiles)}");

                    var libFiles = new List<LibFileEx>();

                    //read the LibFile records
                    var nobjectfiles = C(mwobLibHeader.nobjectfiles);
                    for (int i = 0; i < nobjectfiles; i++)
                    {
                        var libFile = Utils.Read<LibFile>(file);

                        var libFileEx = new LibFileEx { LibFile = libFile, FileName = GetFileName(libFile, file), FullPathName = GetFullPathName(libFile, file), Object = GetObject(libFile, file) };

                        Console.WriteLine($"\tLibFile {i}\r\n\t\tmoddate {ToDateTime(UC(libFile.moddate))}\r\n\t\tfilename (offset) {C(libFile.filename)} : {libFileEx.FileName}\r\n\t\tfullpathname (offset) {C(libFile.fullpathname)} : {libFileEx.FullPathName}\r\n\t\tobjectstart {C(libFile.objectstart)}\r\n\t\tobjectsize {C(libFile.objectsize)}");

                        Console.WriteLine($"\t\t\tObjHeader\r\n\t\t\t\tmagic_word {C(libFileEx.ObjectHeader.magic_word).ToString("X")}\r\n\t\t\t\t...");

                        DumpObject(libFileEx.FileName + ".DUMP.txt", libFileEx.Object);

                        libFileEx.ObjectNameTable = GetObjectNameTable(libFileEx.ObjectHeader, libFileEx.Object);

                        libFiles.Add(libFileEx);
                    }

                    file.Close();
                }

                fs.Close();
            }

            Console.ReadLine();
        }

        static void DumpObject(string fileName, byte[] objectData)
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

        static string[] LiteralGetObjectNameTable(ObjHeader oh, byte[] oa)
        {
            var result = new List<string>();
            var buffer = new StringBuilder();
            for (int i = C(oh.nametable_offset); i < oa.Length; i++)
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

        static string[] GetObjectNameTable(ObjHeader oh, byte[] oa)
        {
            var buffer = new StringBuilder();
            var result = new List<string>();

            var ntoffset = C(oh.nametable_offset);
            var ncount = C(oh.nametable_names);

            //while (ncount > 0)
            //{
            //    var lastcharwasnull = false;
            //    do
            //    {
            //        var ch = (char)oa[ntoffset];
            //        if (lastcharwasnull || ch != '\0')
            //        {
            //            buffer.Append(ch);
            //            lastcharwasnull = false;
            //        }
            //        else
            //            lastcharwasnull = true;

            //        ntoffset++;
            //    } while (!lastcharwasnull && oa[ntoffset] != 0);

            //    result.Add(buffer.ToString());
            //    buffer.Clear();
            //    ncount--;
            //}

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

        static byte[] GetObject(LibFile libFile, BinaryReader file)
        {
            byte[] result = null;
            var current = file.BaseStream.Position;
            try
            {
                file.BaseStream.Position = C(libFile.objectstart);
                var size = C(libFile.objectsize);
                result = file.ReadBytes(size);

                System.Diagnostics.Debug.Assert(result.Length == size);
            }
            finally
            {
                file.BaseStream.Position = current;
            }
            return result;
        }

        static string GetFullPathName(LibFile libFile, BinaryReader file)
        {
            return GetString(file, C(libFile.fullpathname));
        }

        static string GetFileName(LibFile libFile, BinaryReader file)
        {
            return GetString(file, C(libFile.filename));
        }

        static string GetString(BinaryReader file, Int32 fileOffset)
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


    }
}
