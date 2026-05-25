using System;
#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class BC1 : IPixelFormat
    {
        public string Name => "BC1";

        public int Size => 8;

        public byte[] Encode(Color color)
        {
            // BC1 encoding is handled block-level in IMGCWriter.EncodePixels, not per-pixel.
            throw new NotSupportedException("BC1 does not support per-pixel encoding.");
        }

        public Color Decode(byte[] data)
        {
#if USE_SYSTEM_DRAWING
            return Color.FromArgb(255, data[0], data[1], data[2]);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[0], data[1], data[2], 255));
#endif
        }
    }
}