#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using StudioElevenLib.Tools;
using StudioElevenLib.Common.Imaging;
using StudioElevenLib.Level5.Compression;

#if USE_SYSTEM_DRAWING
using System.Drawing;
using System.Drawing.Imaging;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
#endif

namespace StudioElevenLib.Level5.Image.IO
{
    public class Image5Writer
    {
        private readonly Image5 _image;

        public Image5Writer(Image5 image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }

        public void Save(string fileName, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("File name empty", nameof(fileName));
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192);
            WriteToStream(stream, progress);
        }

        public byte[] Save(IProgress<int>? progress = null)
        {
            using var memoryStream = new MemoryStream();
            WriteToStream(memoryStream, progress);
            return memoryStream.ToArray();
        }

        private void WriteToStream(Stream stream, IProgress<int>? progress = null)
        {
            var methodName = $"WriteV{_image.Version}";
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) throw new NotSupportedException($"{_image.Profile.Name} version {_image.Version} not supported.");

            method.Invoke(this, new object[] { stream, progress! });
        }

        private void WriteV0(Stream stream, IProgress<int>? progress = null)
        {
            progress?.Report(0);

            var formatPair = _image.Profile.PixelFormats.FirstOrDefault(kv => kv.Value?.Name == _image.ImageFormat!.Name);
            if (formatPair.Value == null)
                throw new InvalidOperationException($"Error: Format '{_image.ImageFormat!.Name}' is not recognized for {_image.Profile.Name}.");

            byte formatKey = formatPair.Key;
            int bitDepth = _image.ImageFormat!.IsBlock ? _image.ImageFormat.Size : _image.ImageFormat.Size * 8;
            int bytesPerTile = 64 * bitDepth / 8;

            var headerDims = _image.Profile.GetHeaderDimensions(_image.Width, _image.Height, _image.ImageFormat);
            byte[] encodedPixels = EncodePixels(_image.Pixels!, headerDims.Width, headerDims.Height, _image.ImageFormat);
            progress?.Report(30);

            var tableMs = new MemoryStream();
            _image.Profile.WriteTileTable(new BinaryDataWriter(tableMs), encodedPixels, bitDepth, out byte[] uniqueImageData);
            byte[] tileTableData = tableMs.ToArray();

            progress?.Report(60);

            byte[] compressedTable = Compressor.Compress(tileTableData)!;
            byte[] compressedImage = Compressor.Compress(uniqueImageData)!;
            progress?.Report(90);

            int tableSize1 = compressedTable.Length;
            int tableSize2 = (tableSize1 + 3) & ~3;

            var bw = new BinaryDataWriter(stream);
            string verStr = _image.Version.ToString("D2");
            byte b1 = (byte)verStr[0]; byte b2 = (byte)verStr[1];

            var header = new Image5Support.Header
            {
                Magic = _image.Profile.Magic,
                UnkBlock1 = new byte[] { b1, b2, 0x00, 0x00, 0x30, 0x00 },
                ImageFormat = formatKey,
                Unk2 = 0x01,
                CombineFormat = 0x01,
                BitDepth = (byte)bitDepth,
                BytesPerTile = (short)bytesPerTile,
                Width = (short)headerDims.Width,
                Height = (short)headerDims.Height,
                UnkBlock3 = new byte[] { 0x30, 0x00, 0x00, 0x00, 0x30, 0x00, 0x01, 0x00 },
                TileOffset = 0x48,
                UnkBlock4 = new byte[20],
                TileSize1 = tableSize1,
                TileSize2 = tableSize2,
                ImageSize = compressedImage.Length,
                UnkBlock5 = new byte[8]
            };

            bw.WriteStruct(header);
            bw.Write(compressedTable);
            if (tableSize2 > tableSize1) bw.Write(new byte[tableSize2 - tableSize1]);
            bw.Write(compressedImage);
            bw.WriteAlignment(16, 0x00);

            progress?.Report(100);
        }

        private byte[] EncodePixels(Color[] pixels, int headerWidth, int headerHeight, IPixelFormat imgFormat)
        {
            if (imgFormat.IsBlock)
            {
                return _image.Profile.EncodeBlockPixels(pixels, headerWidth, headerHeight, imgFormat);
            }

            var swizzleData = _image.Profile.GetSwizzleSequence(headerWidth, headerHeight);
            var points = swizzleData.Points.ToArray();

            var ms = new MemoryStream();

            foreach (var point in points)
            {
                int x = point.X;
                int y = point.Y;

                Color color = (x < swizzleData.OutputWidth && y < swizzleData.OutputHeight)
                    ? pixels[y * swizzleData.OutputWidth + x]
#if USE_SYSTEM_DRAWING
                    : Color.FromArgb(0, 0, 0, 0);
#elif USE_IMAGESHARP
                    : Color.Transparent;
#endif
                byte[]? encoded = imgFormat.Encode(color);
                if (encoded != null) ms.Write(encoded, 0, encoded.Length);
            }

            return ms.ToArray();
        }
    }
}
