using System.Runtime.InteropServices;

namespace StudioElevenLib.Level5.Material
{
    public class MTRCSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MTRCHeader
        {
            public long Magic;
            public short DataStartOffset;
            public short DataEndOffset;
            private int EmpryBlock1;
            public short DataEndOffset2;
            private short DataEndOffset3;
            public int EmpryBlock2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LUTCHeader
        {
            public long Magic;
            public short EmptyBlock1;
            public short DataStartOffset;
            public int EmptyBlock2;
        }
    }
}
