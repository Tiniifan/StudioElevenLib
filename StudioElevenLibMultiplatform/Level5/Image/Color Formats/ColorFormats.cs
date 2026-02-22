using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace StudioElevenLib.Level5.Image
{
    public class RGBA4 : IColorFormat
    {
        public string Name => "RGBA4";
        public int Size => 2;

        public byte[] Encode(Color color)
        {
            var pixel = color.ToPixel<Rgba32>();
            int r = pixel.R >> 4;
            int g = pixel.G >> 4;
            int b = pixel.B >> 4;
            int a = pixel.A >> 4;

            ushort rgba4 = (ushort)((r << 12) | (g << 8) | (b << 4) | a);
            return new byte[]
            {
                (byte)(rgba4 & 0xFF),
                (byte)(rgba4 >> 8)
            };
        }

        public Color Decode(byte[] data)
        {
            if (data.Length < 2)
                return Color.FromPixel(new Rgba32(0, 0, 0, 0));

            ushort rgba4 = (ushort)((data[1] << 8) | data[0]);

            byte r = (byte)(((rgba4 >> 12) & 0xF) * 17);
            byte g = (byte)(((rgba4 >> 8) & 0xF) * 17);
            byte b = (byte)(((rgba4 >> 4) & 0xF) * 17);
            byte a = (byte)((rgba4 & 0xF) * 17);

            return Color.FromPixel(new Rgba32(r, g, b, a));
        }
    }

    public class RGBA8 : IColorFormat
    {
        public string Name => "RGBA8";
        public int Size => 4;

        public byte[] Encode(Color color)
        {
            var pixel = color.ToPixel<Rgba32>();
            return new byte[] { pixel.A, pixel.R, pixel.G, pixel.B };
        }

        public Color Decode(byte[] data)
        {
            if (data.Length < 4)
                return Color.FromPixel(new Rgba32(0, 0, 0, 0));

            return Color.FromPixel(new Rgba32(data[1], data[2], data[3], data[0]));
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
            return Color.FromPixel(new Rgba32(data[0], data[1], data[2], 255));
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
            return Color.FromPixel(new Rgba32(data[0], data[1], data[2], data[3]));
        }
    }

    public class RBGR888 : IColorFormat
    {
        public string Name => "RBGR888";
        public int Size => 3;

        public byte[] Encode(Color color)
        {
            var pixel = color.ToPixel<Rgba32>();
            return new byte[] { pixel.R, pixel.B, pixel.G };
        }

        public Color Decode(byte[] data)
        {
            if (data.Length < 3)
                return Color.FromPixel(new Rgba32(0, 0, 0, 0));


            byte r = data[2];
            byte g = data[1];
            byte b = data[0];

            return Color.FromPixel(new Rgba32(r, g, b, 255));
        }
    }
}