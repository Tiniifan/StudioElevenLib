#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class RGBA5 : IPixelFormat
    {
        public string Name => "RGBA5";

        public int Size => 2;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            int r = color.R >> 3;
            int g = color.G >> 3;
            int b = color.B >> 3;
            int a = color.A >> 7;
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            int r = pixel.R >> 3;
            int g = pixel.G >> 3;
            int b = pixel.B >> 3;
            int a = pixel.A >> 7;
#endif
            ushort val = (ushort)((r << 11) | (g << 6) | (b << 1) | a);

            byte[] data = new byte[2];
            data[0] = (byte)(val & 0xFF);
            data[1] = (byte)(val >> 8);

            return data;
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

            ushort val = (ushort)((data[1] << 8) | data[0]);

            int r5 = (val >> 11) & 0x1F;
            int g5 = (val >> 6) & 0x1F;
            int b5 = (val >> 1) & 0x1F;
            int a1 = val & 0x1;

#if USE_SYSTEM_DRAWING
            int r = (r5 << 3) | (r5 >> 2);
            int g = (g5 << 3) | (g5 >> 2);
            int b = (b5 << 3) | (b5 >> 2);
            int a = a1 * 255;

            return Color.FromArgb(a, r, g, b);
#elif USE_IMAGESHARP
            byte r = (byte)((r5 << 3) | (r5 >> 2));
            byte g = (byte)((g5 << 3) | (g5 >> 2));
            byte b = (byte)((b5 << 3) | (b5 >> 2));
            byte a = (byte)(a1 * 255);

            return Color.FromPixel(new Rgba32(r, g, b, a));
#endif
        }
    }
}