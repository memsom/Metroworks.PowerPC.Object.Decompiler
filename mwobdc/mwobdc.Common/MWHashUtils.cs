using System;
using System.Linq;

namespace mwobdc.Common
{
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
}
