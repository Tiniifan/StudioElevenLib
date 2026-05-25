using System.Collections.Generic;
using System.Runtime.InteropServices;
using StudioElevenLib.Level5.Image.Color_Formats;

namespace StudioElevenLib.Level5.Image.IMGC
{
    public static class IMGCSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public uint Magic;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x06)]
            public byte[] UnkBlock1;
            public byte ImageFormat;
            public byte Unk2;
            public byte CombineFormat;
            public byte BitDepth;
            public short BytesPerTile;
            public short Width;
            public short Height;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x08)]
            public byte[] UnkBlock3;
            public int TileOffset;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
            public byte[] UnkBlock4;
            public int TileSize1;
            public int TileSize2;
            public int ImageSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x08)]
            public byte[] UnkBlock5;
        }

        public static Dictionary<byte, IPixelFormat> PixelFormats3DS = new Dictionary<byte, IPixelFormat>
        {
            {0x00, new RGBA8888() },
            {0x01, new RGBA4444() },
            {0x02, new RGBA5551() },
            {0x03, new BGR888() },
            {0x04, new RGB565() },
            {0x0A, new LA88() },
            {0x0B, new LA44() },
            {0x0C, new L8() },
            {0x0D, new L4() },
            {0x0E, new A8() },
            {0x0F, new A4() },
            {0x1B, new ETC1() },
            {0x1C, new ETC1A4() },
        };

        public static Dictionary<byte, IPixelFormat> SwitchPixelFormats = new Dictionary<byte, IPixelFormat>
        {
            {0x00, new ABGR8888() },
            {0x03, new RGB888() },
            {0x0E, new A8() },
            {0x1D, new BC1() },
            {0x1F, new BC5() }
        };
    }
}