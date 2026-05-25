#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class RGB565 : IPixelFormat
    {
        public string Name => "RGB565";

        public int Size => 2;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            int r = color.R >> 3;
            int g = color.G >> 2;
            int b = color.B >> 3;
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            int r = pixel.R >> 3;
            int g = pixel.G >> 2;
            int b = pixel.B >> 3;
#endif
            ushort val = (ushort)((r << 11) | (g << 5) | b);

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
            int g6 = (val >> 5) & 0x3F;
            int b5 = val & 0x1F;

#if USE_SYSTEM_DRAWING
            int r = (r5 << 3) | (r5 >> 2);
            int g = (g6 << 2) | (g6 >> 4);
            int b = (b5 << 3) | (b5 >> 2);

            return Color.FromArgb(255, r, g, b);
#elif USE_IMAGESHARP
            byte r = (byte)((r5 << 3) | (r5 >> 2));
            byte g = (byte)((g6 << 2) | (g6 >> 4));
            byte b = (byte)((b5 << 3) | (b5 >> 2));

            return Color.FromPixel(new Rgba32(r, g, b, 255));
#endif
        }
    }
}