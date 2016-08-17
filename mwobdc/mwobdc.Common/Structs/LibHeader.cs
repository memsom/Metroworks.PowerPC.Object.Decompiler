using System;
using System.Runtime.InteropServices;

namespace mwobdc.Common.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LibHeader
    {
        public Int32 magicword;
        public Int32 magicproc;
        public Int32 magicflags;
        public Int32 version;
        public Int32 code_size;
        public Int32 data_size;
        public Int32 nobjectfiles;
    }
}
