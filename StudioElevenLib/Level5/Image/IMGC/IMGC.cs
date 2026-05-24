#nullable enable

using System;
using System.IO;
using StudioElevenLib.Tools;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.IMGC
{
    public class IMGC
    {
        public string Name => "IMGC";

#if USE_SYSTEM_DRAWING
        /// <summary>Decoded bitmap, null until loaded.</summary>
        public Bitmap? Bitmap { get; set; }
#elif USE_IMAGESHARP
        /// <summary>Decoded bitmap, null until loaded.</summary>
        public Image<Rgba32>? Bitmap { get; set; }
#endif

        /// <summary>Flat pixel array in row-major order.</summary>
        public Color[]? Pixels { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>Color format used to encode/decode this image.</summary>
        public IPixelFormat? ImageFormat { get; set; }

        /// <summary>Empty constructor for manual initialization.</summary>
        public IMGC() { }

        /// <summary>Reads and decodes an IMGC image from a stream.</summary>
        public IMGC(Stream stream)
        {
            using (stream)
            {
                var reader = new IMGCReader(stream);
                var result = reader.Read();

                Bitmap = result.bitmap;
                Pixels = result.pixels;
                Width = result.width;
                Height = result.height;
                ImageFormat = result.imageFormat;
            }
        }

        /// <summary>Reads and decodes an IMGC image from a byte array.</summary>
        public IMGC(byte[] fileByteArray)
        {
            using (var ms = new MemoryStream(fileByteArray))
            {
                var reader = new IMGCReader(ms);
                var result = reader.Read();

                Bitmap = result.bitmap;
                Pixels = result.pixels;
                Width = result.width;
                Height = result.height;
                ImageFormat = result.imageFormat;
            }
        }

        /// <summary>Creates an IMGC from an existing bitmap and color format.</summary>
        public IMGC(
#if USE_SYSTEM_DRAWING
            Bitmap bitmap,
#elif USE_IMAGESHARP
            Image<Rgba32> bitmap,
#endif
            IPixelFormat format)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            Bitmap = bitmap;
            ImageFormat = format;
            Width = bitmap.Width;
            Height = bitmap.Height;
            Pixels = new Color[Width * Height];

#if USE_SYSTEM_DRAWING
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    Pixels[y * Width + x] = Color.FromArgb(c.A, c.R, c.G, c.B);
                }
#elif USE_IMAGESHARP
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var c = bitmap[x, y];
                    Pixels[y * Width + x] = Color.FromRgba(c.R, c.G, c.B, c.A);
                }
#endif
        }

        /// <summary>Encodes and saves the image to a file.</summary>
        public void Save(string fileName, IProgress<int>? progress = null)
        {
            var writer = new IMGCWriter(this);
            writer.Save(fileName, progress);
        }

        /// <summary>Encodes the image and returns the bytes.</summary>
        public byte[] Save(IProgress<int>? progress = null)
        {
            var writer = new IMGCWriter(this);
            return writer.Save(progress);
        }

        /// <summary>Releases bitmap and pixel data from memory.</summary>
        public void Close()
        {
            Bitmap?.Dispose();
            Bitmap = null;
            Pixels = null;
        }
    }
}