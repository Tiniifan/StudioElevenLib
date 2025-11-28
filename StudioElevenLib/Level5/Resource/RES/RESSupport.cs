using System.Collections.Generic;
using System.Runtime.InteropServices;
using StudioElevenLib.Level5.Resource;

namespace StudioElevenLib.Level5.Resource.RES
{
    public class RESSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public long Magic;
            public short _stringOffset;
            public short Unk1;
            public short _materialTableOffset;
            public short MaterialTableCount;
            public short _nodeOffset;
            public short NodeCount;

            public int StringOffset => _stringOffset << 2;
            public int MaterialTableOffset => _materialTableOffset << 2;
            public int NodeOffset => _nodeOffset << 2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HeaderTable
        {
            public short _dataOffset;
            public short Count;
            public short Type;
            public short Length;

            public int DataOffset => _dataOffset << 2;
        }

        public static List<RESType> Materials = new List<RESType>
        {
            RESType.MaterialTypeUnk1,
            RESType.Material1,
            RESType.Material2,
            RESType.TextureData,
            RESType.MaterialTypeUnk2,
            RESType.MaterialData,
        };

        public static List<RESType> Nodes = new List<RESType>
        {
            RESType.MeshName,
            RESType.Bone,
            RESType.AnimationMTN2,
            RESType.AnimationMTN3,
            RESType.AnimationIMN2,
            RESType.AnimationMTM2,
            RESType.Shading,
            RESType.NodeTypeUnk2,
            RESType.Properties,
            RESType.MTNINF,
            RESType.MTNINF2,
            RESType.IMMINF,
            RESType.MTMINF,
            RESType.Textproj,
        };
    }
}
