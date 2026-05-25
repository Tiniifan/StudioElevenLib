using System;
#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class ABGR8888 : IPixelFormat
    {
        public string Name => "ABGR8888";

        public int Size => 4;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            return new byte[] { color.A, color.B, color.G, color.R };
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            return new byte[] { pixel.A, pixel.B, pixel.G, pixel.R };
#endif
        }

        public Color Decode(byte[] data)
        {
            if (data.Length < 4)
            {
#if USE_SYSTEM_DRAWING
                return Color.FromArgb(0);
#elif USE_IMAGESHARP
                return Color.FromPixel(new Rgba32(0, 0, 0, 0));
#endif
            }

#if USE_SYSTEM_DRAWING
            return Color.FromArgb(data[0], data[3], data[2], data[1]);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[3], data[2], data[1], data[0]));
#endif
        }
    }
}