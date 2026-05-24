#nullable enable

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
        private readonly IPixelFormat _imgFormat;

        public IMGCWriter(IMGC imgc)
        {
            if (imgc == null) throw new ArgumentNullException(nameof(imgc));
            _pixels = imgc.Pixels!;
            _width = imgc.Width;
            _height = imgc.Height;
            _imgFormat = imgc.ImageFormat!;
        }

        /// <summary>
        /// Encodes a pixel array into the IMGC format and saves the result to a file.
        /// </summary>
        public void Save(string fileName, IProgress<int>? progress = null)
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
        public byte[] Save(IProgress<int>? progress = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                WriteToStream(memoryStream, progress);
                return memoryStream.ToArray();
            }
        }

        private void WriteToStream(Stream stream, IProgress<int>? progress = null)
        {
            progress?.Report(0);

            var formatPair = IMGCSupport.PixelFormats.FirstOrDefault(kv => kv.Value?.Name == _imgFormat.Name);
            byte formatKey = formatPair.Key;

            bool isEtc1 = _imgFormat.Name == "ETC1";
            bool isEtc1a4 = _imgFormat.Name == "ETC1A4";
            int bitDepth = isEtc1 ? 4 : isEtc1a4 ? 8 : _imgFormat.Size * 8;
            int bytesPerTile = 64 * bitDepth / 8;

            byte[] encodedPixels = EncodePixels(_pixels, _width, _height, _imgFormat);
            progress?.Report(30);

            Deflate(encodedPixels, bitDepth, out byte[] tileTableData, out byte[] uniqueImageData);
            progress?.Report(60);

            byte[] compressedTable = Compressor.Compress(tileTableData)!;
            byte[] compressedImage = Compressor.Compress(uniqueImageData)!;
            progress?.Report(90);

            int tableSize1 = compressedTable.Length;
            int tableSize2 = (tableSize1 + 3) & ~3;

            var bw = new BinaryDataWriter(stream);

            var header = new IMGCSupport.Header
            {
                Magic = 0x43474D49u,
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

            bw.WriteStruct(header);
            bw.Write(compressedTable);

            if (tableSize2 > tableSize1)
                bw.Write(new byte[tableSize2 - tableSize1]);

            bw.Write(compressedImage);
            bw.WriteAlignment(16, 0x00);

            progress?.Report(100);
        }

        /// <summary>
        /// Applies the IMGC swizzle and encodes each pixel with the given color format.
        /// </summary>
        private byte[] EncodePixels(Color[] pixels, int width, int height, IPixelFormat imgFormat)
        {
            bool isEtc1 = imgFormat.Name == "ETC1";
            bool isEtc1a4 = imgFormat.Name == "ETC1A4";

            if (isEtc1 || isEtc1a4)
            {
                int paddedW = (width + 7) & ~7;
                int paddedH = (height + 7) & ~7;

                byte[] rgbaData = new byte[paddedW * paddedH * 4];

                for (int y = 0; y < paddedH; y++)
                {
                    for (int x = 0; x < paddedW; x++)
                    {
                        // Get local coordinates within the 4x4 block
                        int blockX = x / 4;
                        int blockY = y / 4;
                        int localX = x % 4;
                        int localY = y % 4;

                        // Apply a local transpose (swap X and Y) to resolve the Z-order conflict 
                        int sampleX = blockX * 4 + localY;
                        int sampleY = blockY * 4 + localX;

                        // We'll find the transposed pixel in the original image
#if USE_SYSTEM_DRAWING
                        Color c = (sampleX < width && sampleY < height)
                                    ? pixels[sampleY * width + sampleX]
                                    : Color.FromArgb(0, 0, 0, 0);
#elif USE_IMAGESHARP
                        Color c = (sampleX < width && sampleY < height)
                                    ? pixels[sampleY * width + sampleX]
                                    : Color.Transparent;
#endif

                        int idx = (y * paddedW + x) * 4;

#if USE_SYSTEM_DRAWING
                        rgbaData[idx] = c.R;
                        rgbaData[idx + 1] = c.G;
                        rgbaData[idx + 2] = c.B;
                        rgbaData[idx + 3] = c.A;
#elif USE_IMAGESHARP
                        var px = c.ToPixel<Rgba32>();
                        rgbaData[idx] = px.R;
                        rgbaData[idx + 1] = px.G;
                        rgbaData[idx + 2] = px.B;
                        rgbaData[idx + 3] = px.A;
#endif
                    }
                }

                // Compress the transposed image 
                byte[] linearCompressed = EtcpakTool.Compress(rgbaData, paddedW, paddedH, isEtc1a4)!;
                byte[] swizzledCompressed = new byte[linearCompressed.Length];

                int blockSizeBytes = isEtc1 ? 8 : 16;
                int blocksX = paddedW / 4;

                var swizzle = new IMGCSwizzle(width, height);
                var points = swizzle.GetPointSequence().ToArray();

                int numBlocks = linearCompressed.Length / blockSizeBytes;

                for (int i = 0; i < numBlocks; i++)
                {
                    var pt = points[i * 16];
                    int blockX = pt.X / 4;
                    int blockY = pt.Y / 4;

                    int linearBlockIndex = blockY * blocksX + blockX;

                    Array.Copy(
                        linearCompressed,
                        linearBlockIndex * blockSizeBytes,
                        swizzledCompressed,
                        i * blockSizeBytes,
                        blockSizeBytes
                    );
                }

                return swizzledCompressed;
            }

            // Other pixel format
            var swizzleStd = new IMGCSwizzle(width, height);
            var pointsStd = swizzleStd.GetPointSequence().ToArray();

            int paddedWidthStd = (width + 7) & ~7;
            int paddedHeightStd = (height + 7) & ~7;
            int count = paddedWidthStd * paddedHeightStd;

            var ms = new MemoryStream(count * imgFormat.Size);

            for (int i = 0; i < count; i++)
            {
                var point = pointsStd[i];
                Color color;
                if (point.X < width && point.Y < height)
                    color = pixels[point.Y * width + point.X];
                else
#if USE_SYSTEM_DRAWING
                    color = Color.FromArgb(0);
#elif USE_IMAGESHARP
                    color = Color.Transparent;
#endif

                byte[]? encoded = imgFormat.Encode(color);
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
                    // Block already known -> write its index
                    bw.Write((ushort)existingIndex);
                }
                else
                {
                    // New block -> add it to the unique list and write index
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