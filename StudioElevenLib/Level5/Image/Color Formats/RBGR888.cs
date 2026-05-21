#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class RBGR888 : IColorFormat
    {
        public string Name => "RBGR888";

        public int Size => 3;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            return new byte[]
            {
                color.R,
                color.B,
                color.G
            };
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            return new byte[]
            {
                pixel.R,
                pixel.B,
                pixel.G
            };
#endif
        }

        public Color Decode(byte[] data)
        {
            if (data.Length < 3)
            {
#if USE_SYSTEM_DRAWING
                return Color.FromArgb(0);
#elif USE_IMAGESHARP
                return Color.FromPixel(new Rgba32(0, 0, 0, 0));
#endif
            }

#if USE_SYSTEM_DRAWING
            return Color.FromArgb(255, data[2], data[1], data[0]);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[2], data[1], data[0], 255));
#endif
        }
    }
}
