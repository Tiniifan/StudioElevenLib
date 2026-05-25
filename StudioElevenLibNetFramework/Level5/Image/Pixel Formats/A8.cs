#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class A8 : IPixelFormat
    {
        public string Name => "A8";

        public int Size => 1;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            return new byte[] { color.A };
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            return new byte[] { pixel.A };
#endif
        }

        public Color Decode(byte[] data)
        {
            if (data.Length < 1)
            {
#if USE_SYSTEM_DRAWING
                return Color.FromArgb(0);
#elif USE_IMAGESHARP
                return Color.FromPixel(new Rgba32(0, 0, 0, 0));
#endif
            }

            byte a = data[0];

#if USE_SYSTEM_DRAWING
            return Color.FromArgb(a, 255, 255, 255);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(255, 255, 255, a));
#endif
        }
    }
}