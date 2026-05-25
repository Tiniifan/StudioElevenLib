using System;
#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class BC5 : IPixelFormat
    {
        public string Name => "BC5";

        public int Size => 16;

        public byte[] Encode(Color color)
        {
            // BC5 encoding is handled block-level in IMGCWriter.EncodePixels, not per-pixel.
            throw new NotSupportedException("BC5 does not support per-pixel encoding.");
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