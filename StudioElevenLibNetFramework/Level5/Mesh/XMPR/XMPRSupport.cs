using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Mesh.XMPR
{
    public static class XMPRSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct XMPRHeader
        {
            public uint Magic; // "XMPR" (0x52504D58)
            public uint Size1; // 64
            public uint XprmLength;
            public uint PropertiesOffset;
            public uint UnkOffset;
            public uint UnkLength;
            public uint Unk1Offset;
            public uint Unk1Length;
            public uint Unk2Offset;
            public uint Unk2Length;
            public uint NodesOffset;
            public uint NodesLength;
            public uint MeshNameOffset;
            public uint MeshNameLength;
            public uint MaterialNameOffset;
            public uint MaterialNameLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct XPRMHeader
        {
            public uint Magic; // "XPRM" (0x4D525058)
            public uint XpvbOffset;
            public uint XpvbLength;
            public uint XpviOffset;
            public uint XpviLength;
        }
    }
}
