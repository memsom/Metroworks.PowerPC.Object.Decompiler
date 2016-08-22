using System;
using System.Collections.Generic;
using System.IO;
using mwobdc.Common;
using mwobdc.Common.Structs;
using System.Runtime.InteropServices;

namespace mwobdc
{
    class Program
    {
        static Int32 C(Int32 value)
        {
            return Utils.SwapInt32(value);
        }

        static UInt32 UC(UInt32 value)
        {
            return Utils.SwapUInt32(value);
        }

        static void Main(string[] args)
        {
            //using (var fs = new FileStream("mslstdrt.o", FileMode.Open))
            //using (var fs = new FileStream("start_dyn.o", FileMode.Open))
            using (var fs = new FileStream("test.o", FileMode.Open))
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

                        var libFileEx = new LibFileEx
                        {
                            LibFile = libFile,
                            FileName = libFile.GetFileName(file),
                            FullPathName = libFile.GetFullPathName(file), //for BeOS, this seems to be 0 (zero)... I think they left out the redundant extra name
                            Object = libFile.GetObject(file)
                        };

                        Console.WriteLine($"\tLibFile {i}\r\n\t\tmoddate {Utils.ToDateTime(UC(libFile.moddate))}\r\n\t\tfilename (offset) {C(libFile.filename)} : {libFileEx.FileName}\r\n\t\tfullpathname (offset) {C(libFile.fullpathname)} : {libFileEx.FullPathName}\r\n\t\tobjectstart {C(libFile.objectstart)}\r\n\t\tobjectsize {C(libFile.objectsize)}");

                        Console.WriteLine($"\t\t\tObjHeader\r\n\t\t\t\tmagic_word {C(libFileEx.ObjectHeader.Value.magic_word).ToString("X")}\r\n\t\t\t\t...");

                        //dump the objects to files... this makes working out the contents simpler
                        Utils.DumpObject(libFileEx.FileName.Replace('/', '+') + ".DUMP.txt", libFileEx.Object);

                        //grab the name table
                        Console.WriteLine($"\t\t\t\tNameTable");
                        var ont = libFileEx.ObjectNameTable;
                        foreach (var nte in ont)
                        {
                            var valid = nte.validated ? "+" : "-";
                            Console.WriteLine($"\t\t\t\t\t{nte.offset.ToString("x")}:: {nte.name} {nte.check_sum.ToString("x")}[{valid}]");
                        }

                        Utils.DumpObjectContents(libFileEx.FileName.Replace('/', '+'), libFileEx.Object, libFileEx.ObjectNameTable); //, Marshal.SizeOf(typeof(ObjHeader)));

                        libFiles.Add(libFileEx);
                    }

                    file.Close();
                }

                fs.Close();
            }

            Console.ReadLine();
        }
    }
}
