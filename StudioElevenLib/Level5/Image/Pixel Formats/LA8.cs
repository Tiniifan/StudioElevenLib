#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class LA8 : IPixelFormat
    {
        public string Name => "LA8";

        public int Size => 2;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            byte l = (byte)((color.R + color.G + color.B) / 3);
            byte a = color.A;
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            byte l = (byte)((pixel.R + pixel.G + pixel.B) / 3);
            byte a = pixel.A;
#endif
            return new byte[] { l, a };
        }

        public Color Decode(byte[] data)
        {
            if (data.Length < 2)
            {
#if USE_SYSTEM_DRAWING
                return Color.FromArgb(0);
#elif USE_IMAGESHARP
                return Color.FromPixel(new Rgba32(0, 0, 0, 0));
#endif
            }

            byte l = data[0];
            byte a = data[1];

#if USE_SYSTEM_DRAWING
            return Color.FromArgb(a, l, l, l);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(l, l, l, a));
#endif
        }
    }
}