using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace mwobdc.Common.Structs
{
    public enum Hunk
    {
        HUNK_START = 0x4567,        //Always the first hunk in any object file. Singleton. Defines as ObjMiscHunk
        HUNK_END,         //4568    //Always the last hunk in an object file. Singleton. Defines as ObjMiscHunk
        HUNK_SEGMENT,
        HUNK_LOCAL_CODE,  //456a    //Defines code for a function with static (i.e. internal) linkage, followed immediately by machine code. Defined as an ObjCodeHunk
        HUNK_GLOBAL_CODE, //456b    //Defines code for a function with external linkage, followed immediately by machine code. Defined as an ObjCodeHunk
        HUNK_LOCAL_UDATA,
        HUNK_GLOBAL_UDATA,
        HUNK_LOCAL_IDATA, //456e
        HUNK_GLOBAL_IDATA,//456f
        HUNK_GLOBAL_ENTRY,
        HUNK_LOCAL_ENTRY,
        HUNK_IMPORT,
        HUNK_XREF_16BIT,
        HUNK_XREF_16BIT_IL,
        HUNK_XREF_24BIT,  //4575
        HUNK_XREF_32BIT,
        HUNK_XREF_32BIT_REL,
        HUNK_DEFINIT,               //Reserved
        HUNK_LIBRARY_BREAK,         //Obsolete
        HUNK_IMPORT_CONTAINER,
        HUNK_SOURCE_BREAK,
        HUNK_XREF_16BIT_REL,
        HUNK_METHOD_REF,
        HUNK_CLASS_DEF,
        HUNK_FORCE_ACTIVE //457f    //Forces the linker to never dead strip out the object defined by the following hunk
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjPeekHunk
    {
        public Int16 hunk_type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjMiscHunk
    {
        public Int16 hunk_type;
        public Int16 unused; //Padding
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjCodeHunk
    {
        public Int16 hunk_type;
        public SByte sm_class;
        public Byte x;
        public Int32 name_id;
        public Int32 size;
        public Int32 sym_offset;
        public Int32 sym_decl_offset;
    }

    /// <summary>
    /// Okay - can you see the difference between ObjCodeHunk and ObjDataHunk?! Yeah.. one field name...
    /// Legacy code is simply weird and awesome.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjDataHunk
    {
        public Int16 hunk_type;
        public SByte sm_class;
        public Byte x;
        public Int32 name_id;
        public Int32 size;
        public Int32 sym_type_id;
        public Int32 sym_decl_offset;
    }

    /// <summary>
    /// Okay - can you see the difference between ObjCodeHunk and ObjDataHunk?! Yeah.. one field name...
    /// Legacy code is simply weird and awesome.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjEntryHunk
    {
        public Int16 hunk_type;
        public Int16 unused;
        public Int32 name_id;
        public Int32 offset;
        public Int32 sym_type_id;
        public Int32 sym_decl_offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjXRefHunk
    {
        public Int16 hunk_type;
        public SByte sm_class;
        public Byte unused;
        public Int32 name_id;
        public Int32 offset;
    }

    /// <summary>
    /// These represent the only valid values for ObjCodeHunk.sm_class
    /// </summary>
    public static class PowerPCConsts
    {
        /* read only classes */
        public const sbyte XMC_PR = 0;   //program code
        public const sbyte XMC_RO = 1;   //read only constant
        public const sbyte XMC_GL = 6;   //global linkage

        /* read/write classes */
        public const sbyte XMC_RW = 5;   //read write data
        public const sbyte XMC_TC0 = 15; //TOC anchor
        public const sbyte XMC_TC = 3;  //general TOC entry
        public const sbyte XMC_TD = 16;  //scalar TOC data
        public const sbyte XMC_DS = 10;  //routine descriptor

        /* possible BeOS specific */
        public const sbyte XMC_UNK0 = 12; //this seems to be a BeOS specific value...not in the original Spec.
        public const sbyte XMC_UNK1 = 13; //this seems to be a BeOS specific value...not in the original Spec.
    }
}
