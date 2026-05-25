#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class RGBA8888 : IPixelFormat
    {
        public string Name => "RGBA8888";

        public int Size => 4;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            int argb = color.ToArgb();
            return new byte[] { (byte)((argb >> 24) & 0xFF), (byte)(argb & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)((argb >> 16) & 0xFF) };
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            return new byte[] { pixel.A, pixel.R, pixel.G, pixel.B };
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
            int argb = (data[0] << 24) | (data[3] << 16) | (data[2] << 8) | data[1];
            return Color.FromArgb(argb);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[1], data[2], data[3], data[0]));
#endif
        }
    }
}
