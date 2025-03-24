using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StudioElevenLib.Level5.Resource.XRES
{
    public class XRESSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public uint Magic;
            public short StringOffset;
            public short Unk1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x0C)]
            public byte[] EmptyBlock1;
            public HeaderTable MaterialTypeUnk1;
            public HeaderTable Material1;
            public HeaderTable Material2;
            public HeaderTable TextureData;
            public HeaderTable MaterialTypeUnk2;
            public int EmptyBlock2;
            public HeaderTable MaterialData;
            public int EmptyBlock3;
            public HeaderTable MeshName;
            public HeaderTable Bone;
            public HeaderTable AnimationMTN2;
            public HeaderTable AnimationIMN2;
            public HeaderTable AnimationMTM2;
            public HeaderTable Shading;
            public HeaderTable NodeTypeUnk1;
            public HeaderTable Properties;
            public HeaderTable MTNINF;
            public HeaderTable IMMINF;
            public HeaderTable MTMINF;
            public HeaderTable Textproj;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HeaderTable
        {
            public short DataOffset;
            public short Count;
        }

        public static Dictionary<RESType, int> TypeLength = new Dictionary<RESType, int>
        {
            {RESType.Bone, 8 },
            {RESType.Textproj, 8 },
            {RESType.Properties, 8 },
            {RESType.Shading, 8 },
            {RESType.Material1, 8 },
            {RESType.Material2, 8 },
            {RESType.MeshName, 8 },
            {RESType.TextureData, 32 },
            {RESType.MaterialData, 224 },
            {RESType.AnimationMTN2, 8 },
            {RESType.AnimationIMN2, 8 },
            {RESType.AnimationMTM2, 8 },
            {RESType.MTNINF, 8 },
            {RESType.IMMINF, 8 },
            {RESType.MTMINF, 8 },
        };

        public static List<RESType> TypeOrder = new List<RESType>
        {
            RESType.MaterialTypeUnk1,
            RESType.Material1,
            RESType.Material2,
            RESType.TextureData,
            RESType.MaterialTypeUnk2,
            RESType.MaterialData,
            RESType.MeshName,
            RESType.Bone,
            RESType.AnimationMTN2,
            RESType.AnimationIMN2,
            RESType.AnimationMTM2,
            RESType.Shading,
            RESType.NodeTypeUnk1,
            RESType.Properties,
            RESType.MTNINF,
            RESType.IMMINF,
            RESType.MTMINF,
            RESType.Textproj,
        };

        public static List<RESType> DataOrder = new List<RESType>
        {
            RESType.TextureData,
            RESType.Material1,
            RESType.Material2,
            RESType.MaterialTypeUnk1,
            RESType.MaterialTypeUnk2,
            RESType.MaterialData,
            RESType.MeshName,
            RESType.Bone,
            RESType.AnimationMTN2,
            RESType.AnimationIMN2,
            RESType.AnimationMTM2,
            RESType.Shading,
            RESType.NodeTypeUnk1,
            RESType.Properties,
            RESType.MTNINF,
            RESType.IMMINF,
            RESType.MTMINF,
            RESType.Textproj,
        };
    }
}
