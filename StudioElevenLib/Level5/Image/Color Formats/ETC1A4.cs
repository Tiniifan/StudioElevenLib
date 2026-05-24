using System;
#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class ETC1A4 : IColorFormat
    {
        public string Name => "ETC1A4";

        public int Size => 4;

        public byte[] Encode(Color color)
        {
            // ETC1 encoding is handled block-level in IMGCWriter.EncodePixels, not per-pixel.
            throw new NotSupportedException("ETC1 does not support per-pixel encoding.");
        }

        public Color Decode(byte[] data)
        {
#if USE_SYSTEM_DRAWING
            return Color.FromArgb(data[3], data[0], data[1], data[2]);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[0], data[1], data[2], data[3]));
#endif
        }
    }
}
