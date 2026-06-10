#nullable enable

using System;
using System.IO;

using StudioElevenLib.Common.Imaging;
using StudioElevenLib.Level5.Image.IO;
using StudioElevenLib.Level5.Image.Profiles;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image
{
    public class Image5
    {
        public string Name => Profile.Name;

        /// <summary>Image version extracted from header (e.g. 00 -> 0)</summary>
        public int Version { get; set; } = 0;

        public Image5Profile Profile { get; set; }

#if USE_SYSTEM_DRAWING
        /// <summary>Decoded bitmap, null until loaded.</summary>
        public Bitmap? Bitmap { get; set; }
#elif USE_IMAGESHARP
        /// <summary>Decoded bitmap, null until loaded.</summary>
        public SixLabors.ImageSharp.Image<Rgba32>? Bitmap { get; set; }
#endif

        /// <summary>Flat pixel array in row-major order.</summary>
        public Color[]? Pixels { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>Color format used to encode/decode this image.</summary>
        public IPixelFormat? ImageFormat { get; set; }

        /// <summary>Empty constructor for manual initialization.</summary>
        public Image5(Image5Profile profile)
        {
            Profile = profile;
        }

        /// <summary>Reads and decodes an image from a stream.</summary>
        public Image5(Stream stream, Image5Profile profile, int version = 0)
        {
            Profile = profile;
            Version = version;
            var reader = new Image5Reader(stream, this);
            reader.Read();
        }

        /// <summary>Reads and decodes an image from a byte array.</summary>
        public Image5(byte[] fileByteArray, Image5Profile profile, int version = 0)
        {
            Profile = profile;
            Version = version;
            using var ms = new MemoryStream(fileByteArray);
            var reader = new Image5Reader(ms, this);
            reader.Read();
        }

        /// <summary>Creates an image from an existing bitmap and color format.</summary>
        public Image5(
#if USE_SYSTEM_DRAWING
            Bitmap bitmap,
#elif USE_IMAGESHARP
            SixLabors.ImageSharp.Image<Rgba32> bitmap,
#endif
            IPixelFormat format, Image5Profile profile, int version = 0)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            Profile = profile;
            Version = version;
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
            var writer = new Image5Writer(this);
            writer.Save(fileName, progress);
        }

        /// <summary>Encodes the image and returns the bytes.</summary>
        public byte[] Save(IProgress<int>? progress = null)
        {
            var writer = new Image5Writer(this);
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