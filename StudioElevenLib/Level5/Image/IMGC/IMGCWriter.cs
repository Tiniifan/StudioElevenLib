using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;


#if USE_SYSTEM_DRAWING
using System.Drawing;
using System.Drawing.Imaging;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.IMGC
{
    public class IMGCWriter
    {
        private readonly Color[] _pixels;
        private readonly int _width;
        private readonly int _height;
        private readonly IColorFormat _imgFormat;

        public IMGCWriter(IMGC imgc)
        {
            if (imgc == null) throw new ArgumentNullException(nameof(imgc));
            _pixels = imgc.Pixels;
            _width = imgc.Width;
            _height = imgc.Height;
            _imgFormat = imgc.ImageFormat;
        }

        /// <summary>
        /// Encodes a pixel array into the IMGC format and saves the result to a file.
        /// </summary>
        public void Save(string fileName, IProgress<int> progress = null)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192))
            {
                WriteToStream(stream, progress);
            }
        }

        /// <summary>
        /// Encodes a pixel array into the IMGC format and returns the file bytes.
        /// </summary>
        public byte[] Save(IProgress<int> progress = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                WriteToStream(memoryStream, progress);
                return memoryStream.ToArray();
            }
        }

        private void WriteToStream(Stream stream, IProgress<int> progress = null)
        {
            progress?.Report(0);

            // Find the format key in the dictionary using LINQ
            var formatPair = IMGCSupport.ImageFormats.FirstOrDefault(kv => kv.Value.Name == _imgFormat.Name);
            byte formatKey = formatPair.Key; // Defaults to 0 if not found

            int bitDepth = _imgFormat.Size * 8;
            int bytesPerTile = _imgFormat.Size * 64;

            // Encode pixels (swizzle + color format)
            byte[] encodedPixels = EncodePixels(_pixels, _width, _height, _imgFormat);

            progress?.Report(30);

            // Deduplicate tiles → index table + unique image data
            Deflate(encodedPixels, bitDepth, out byte[] tileTableData, out byte[] uniqueImageData);

            progress?.Report(60);

            // Compress both blocks
            byte[] compressedTable = Compressor.Compress(tileTableData);
            byte[] compressedImage = Compressor.Compress(uniqueImageData);

            progress?.Report(90);

            int tableSize1 = compressedTable.Length;
            int tableSize2 = (tableSize1 + 3) & ~3;   // 4-byte alignment

            // Construct the file
            var bw = new BinaryDataWriter(stream);

            var header = new IMGCSupport.Header
            {
                Magic = 0x43474D49u, // "IMGC"
                UnkBlock1 = new byte[] { 0x30, 0x30, 0x00, 0x00, 0x30, 0x00 },
                ImageFormat = formatKey,
                Unk2 = 0x01,
                CombineFormat = 0x01,
                BitDepth = (byte)bitDepth,
                BytesPerTile = (short)bytesPerTile,
                Width = (short)_width,
                Height = (short)_height,
                UnkBlock3 = new byte[] { 0x30, 0x00, 0x00, 0x00, 0x30, 0x00, 0x01, 0x00 },
                TileOffset = 0x48,
                UnkBlock4 = new byte[20],
                TileSize1 = tableSize1,
                TileSize2 = tableSize2,
                ImageSize = compressedImage.Length,
                UnkBlock5 = new byte[8]
            };

            // Write the entire mapped header at once
            bw.WriteStruct(header);

            // Data
            bw.Write(compressedTable);

            // Padding to reach tableSize2 (aligns the table block to 4 bytes)
            if (tableSize2 > tableSize1)
            {
                bw.Write(new byte[tableSize2 - tableSize1]);
            }

            bw.Write(compressedImage);

            // Final alignment to 16 bytes
            bw.WriteAlignment(16, 0x00);

            progress?.Report(100);
        }

        /// <summary>
        /// Applies the IMGC swizzle and encodes each pixel with the given color format.
        /// </summary>
        private byte[] EncodePixels(Color[] pixels, int width, int height, IColorFormat imgFormat)
        {
            var swizzle = new IMGCSwizzle(width, height);
            var points = swizzle.GetPointSequence().ToArray();

            int paddedWidth = (width + 7) & ~7;
            int paddedHeight = (height + 7) & ~7;
            int pixelCount = paddedWidth * paddedHeight;

            var ms = new MemoryStream(pixelCount * imgFormat.Size);

            for (int i = 0; i < pixelCount; i++)
            {
                var point = points[i];

                Color color;
                if (point.X < width && point.Y < height)
                {
                    color = pixels[point.Y * width + point.X];
                }
                else
                {
                    // Padding pixels (out of bounds areas after 8x8 alignment)
#if USE_SYSTEM_DRAWING
                    color = Color.FromArgb(0);
#elif USE_IMAGESHARP
                    color = Color.FromPixel(new Rgba32(0, 0, 0, 0));
#endif
                }

                byte[] encoded = imgFormat.Encode(color);
                if (encoded != null)
                    ms.Write(encoded, 0, encoded.Length);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Breaks down encoded data into 8x8 tiles, deduplicates them,
        /// and outputs the index table and the unique image buffer without duplicates.
        /// </summary>
        private void Deflate(byte[] encodedData, int bitDepth,
                                     out byte[] tileTable, out byte[] uniqueImageData)
        {
            int blockSize = 64 * bitDepth / 8;

            var tableMs = new MemoryStream();
            var imageMs = new MemoryStream();
            var uniqueBlocks = new List<byte[]>();

            var bw = new BinaryDataWriter(tableMs);

            for (int offset = 0; offset < encodedData.Length; offset += blockSize)
            {
                // Extract the block (potentially partial at the end of the data)
                byte[] block = new byte[blockSize];
                int copyLen = Math.Min(blockSize, encodedData.Length - offset);
                Array.Copy(encodedData, offset, block, 0, copyLen);

                int existingIndex = uniqueBlocks.FindIndex(b => b.SequenceEqual(block));
                if (existingIndex >= 0)
                {
                    // Block already known → write its index
                    bw.Write((ushort)existingIndex);
                }
                else
                {
                    // New block → add it to the unique list and write index
                    bw.Write((ushort)uniqueBlocks.Count);
                    uniqueBlocks.Add(block);
                    imageMs.Write(block, 0, blockSize);
                }
            }

            tileTable = tableMs.ToArray();
            uniqueImageData = imageMs.ToArray();
        }
    }
}
