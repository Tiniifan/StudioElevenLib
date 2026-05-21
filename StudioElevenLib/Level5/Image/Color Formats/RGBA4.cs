#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class RGBA4 : IColorFormat
    {
        public string Name => "RGBA4";

        public int Size => 2;

        public byte[] Encode(Color color)
        {
#if USE_SYSTEM_DRAWING
            int r = color.R >> 4;
            int g = color.G >> 4;
            int b = color.B >> 4;
            int a = color.A >> 4;
#elif USE_IMAGESHARP
            var pixel = color.ToPixel<Rgba32>();
            int r = pixel.R >> 4;
            int g = pixel.G >> 4;
            int b = pixel.B >> 4;
            int a = pixel.A >> 4;
#endif
            ushort rgba4 = (ushort)((r << 12) | (g << 8) | (b << 4) | a);

            byte[] data = new byte[2];
            data[0] = (byte)(rgba4 & 0xFF);
            data[1] = (byte)(rgba4 >> 8);

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

            ushort rgba4 = (ushort)((data[1] << 8) | data[0]);

#if USE_SYSTEM_DRAWING
            int r = ((rgba4 >> 12) & 0xF) * 16;
            int g = ((rgba4 >> 8) & 0xF) * 16;
            int b = ((rgba4 >> 4) & 0xF) * 16;
            int a = (rgba4 & 0xF) * 16;

            return Color.FromArgb(a, r, g, b);
#elif USE_IMAGESHARP
            byte r = (byte)(((rgba4 >> 12) & 0xF) * 17);
            byte g = (byte)(((rgba4 >> 8) & 0xF) * 17);
            byte b = (byte)(((rgba4 >> 4) & 0xF) * 17);
            byte a = (byte)((rgba4 & 0xF) * 17);

            return Color.FromPixel(new Rgba32(r, g, b, a));
#endif
        }
    }
}
