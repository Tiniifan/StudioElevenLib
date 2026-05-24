#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class A4 : IPixelFormat
    {
        public string Name => "A4";

        public int Size => 1;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            return new byte[] { (byte)(color.A >> 4) };
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            return new byte[] { (byte)(pixel.A >> 4) };
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

#if USE_SYSTEM_DRAWING
            int a = (data[0] & 0xF) * 17;

            return Color.FromArgb(a, 255, 255, 255);
#elif USE_IMAGESHARP
            byte a = (byte)((data[0] & 0xF) * 17);

            return Color.FromPixel(new Rgba32(255, 255, 255, a));
#endif
        }
    }
}