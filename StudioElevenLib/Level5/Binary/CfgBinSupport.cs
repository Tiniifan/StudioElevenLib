using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Binary
{
    public class CfgBinSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public int EntriesCount;
            public int StringTableOffset;
            public int StringTableLength;
            public int StringTableCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct KeyHeader
        {
            public int KeyLength;
            public int KeyCount;
            public int KeyStringOffset;
            public int keyStringLength;
        }
    }
}
