#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class L4 : IPixelFormat
    {
        public string Name => "L4";

        public int Size => 1;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            int l = ((color.R + color.G + color.B) / 3) >> 4;
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            int l = ((pixel.R + pixel.G + pixel.B) / 3) >> 4;
#endif
            return new byte[] { (byte)l };
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

#if USE_SYSTEM_DRAWING
            int l = (data[0] & 0xF) * 17;

            return Color.FromArgb(255, l, l, l);
#elif USE_IMAGESHARP
            byte l = (byte)((data[0] & 0xF) * 17);

            return Color.FromPixel(new Rgba32(l, l, l, 255));
#endif
        }
    }
}