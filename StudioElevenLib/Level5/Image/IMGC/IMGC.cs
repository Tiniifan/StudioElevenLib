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
        public Bitmap Bitmap { get; set; }
#elif USE_IMAGESHARP
        public Image<Rgba32> Bitmap { get; set; }
#endif

        public Color[] Pixels { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public IColorFormat ImageFormat { get; set; }

        public IMGC()
        {
        }

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

        public void Save(string fileName, IProgress<int> progress = null)
        {
            var writer = new IMGCWriter(this);
            writer.Save(fileName, progress);
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            var writer = new IMGCWriter(this);
            return writer.Save(progress);
        }

        public void Close()
        {
#if USE_SYSTEM_DRAWING
            Bitmap?.Dispose();
#elif USE_IMAGESHARP
            Bitmap?.Dispose();
#endif
            Bitmap = null;
            Pixels = null;
        }
    }
}