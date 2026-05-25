#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class LA44 : IPixelFormat
    {
        public string Name => "LA44";

        public int Size => 1;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            int l = ((color.R + color.G + color.B) / 3) >> 4;
            int a = color.A >> 4;
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            int l = ((pixel.R + pixel.G + pixel.B) / 3) >> 4;
            int a = pixel.A >> 4;
#endif
            return new byte[] { (byte)((l << 4) | a) };
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

            int val = data[0];

#if USE_SYSTEM_DRAWING
            int l = ((val >> 4) & 0xF) * 17;
            int a = (val & 0xF) * 17;

            return Color.FromArgb(a, l, l, l);
#elif USE_IMAGESHARP
            byte l = (byte)(((val >> 4) & 0xF) * 17);
            byte a = (byte)((val & 0xF) * 17);

            return Color.FromPixel(new Rgba32(l, l, l, a));
#endif
        }
    }
}