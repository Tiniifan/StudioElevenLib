#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image
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

    public class RGBA8 : IColorFormat
    {
        public string Name => "RGBA8";

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

    public class ETC1 : IColorFormat
    {
        public string Name => "ETC1";

        public int Size => 3;

        public byte[] Encode(Color color)
        {
            // Not implemented
            return null;
        }

        public Color Decode(byte[] data)
        {
#if USE_SYSTEM_DRAWING
            return Color.FromArgb(255, data[0], data[1], data[2]);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[0], data[1], data[2], 255));
#endif
        }
    }

    public class ETC1A4 : IColorFormat
    {
        public string Name => "ETC1A4";

        public int Size => 4;

        public byte[] Encode(Color color)
        {
            // Not implemented
            return null;
        }

        public Color Decode(byte[] data)
        {
#if USE_SYSTEM_DRAWING
            return Color.FromArgb(data[3], data[0], data[1], data[2]);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[0], data[1], data[2], data[3]));
#endif
        }
    }

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