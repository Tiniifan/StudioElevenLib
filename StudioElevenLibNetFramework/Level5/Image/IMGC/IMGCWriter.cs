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
        public void Save(string fileName, bool isSwitch = false, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192))
            {
                WriteToStream(stream, isSwitch, progress);
            }
        }

        /// <summary>
        /// Encodes a pixel array into the IMGC format and returns the file bytes.
        /// </summary>
        public byte[] Save(bool isSwitch = false, IProgress<int>? progress = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                WriteToStream(memoryStream, isSwitch, progress);
                return memoryStream.ToArray();
            }
        }

        private void WriteToStream(Stream stream, bool isSwitch = false, IProgress<int>? progress = null)
        {
            progress?.Report(0);

            // Format verification and selection
            KeyValuePair<byte, IPixelFormat> formatPair;
            if (isSwitch)
            {
                formatPair = IMGCSupport.SwitchPixelFormats.FirstOrDefault(kv => kv.Value?.Name == _imgFormat.Name);
                if (formatPair.Value == null)
                {
                    throw new InvalidOperationException($"Error: Compression format '{_imgFormat.Name}' is not supported for the Switch format.");
                }
            }
            else
            {
                formatPair = IMGCSupport.PixelFormats3DS.FirstOrDefault(kv => kv.Value?.Name == _imgFormat.Name);
                if (formatPair.Value == null)
                {
                    throw new InvalidOperationException($"Error: Format '{_imgFormat.Name}' is not recognized for the 3DS format.");
                }
            }

            byte formatKey = formatPair.Key;

            bool isEtc1 = _imgFormat.Name == "ETC1" || _imgFormat.Name == "ETC1A4";
            bool hasAlpha = _imgFormat.Name == "ETC1A4";

            bool isDxt = _imgFormat.Name == "BC1" || _imgFormat.Name == "BC5";
            int version = _imgFormat.Name == "BC1" ? 1 : _imgFormat.Name == "BC5" ? 5 : 0;

            // Calculating the bitDepth
            int bitDepth;
            if (isEtc1 || isDxt)
            {
                bitDepth = 4;

                if (hasAlpha || version >= 5)
                {
                    bitDepth += 4;
                }
            }
            else
            {
                bitDepth = _imgFormat.Size * 8;
            }

            int bytesPerTile = 64 * bitDepth / 8;

            byte[] encodedPixels = EncodePixels(_pixels, _width, _height, _imgFormat, isSwitch);
            progress?.Report(30);

            Deflate(encodedPixels, bitDepth, isSwitch, out byte[] tileTableData, out byte[] uniqueImageData);
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
        private byte[] EncodePixels(Color[] pixels, int width, int height, IPixelFormat imgFormat, bool isSwitch)
        {
            bool isEtc1 = imgFormat.Name == "ETC1" || imgFormat.Name == "ETC1A4";
            bool hasAlpha = imgFormat.Name == "ETC1A4";

            bool isDxt = imgFormat.Name == "BC1" || imgFormat.Name == "BC5";
            int version = imgFormat.Name == "BC1" ? 1 : imgFormat.Name == "BC5" ? 5 : 0;

            bool isBlockFormat = isEtc1 || isDxt;

            if (isBlockFormat)
            {
                int paddedW = (width + 7) & ~7;
                int paddedH = (height + 7) & ~7;

                byte[] rgbaData = new byte[paddedW * paddedH * 4];

                for (int y = 0; y < paddedH; y++)
                {
                    for (int x = 0; x < paddedW; x++)
                    {
                        int idx = (y * paddedW + x) * 4;
                        Color c;

                        if (isSwitch)
                        {
                            // The Switch's block images are linear so there is no local transposition
                            if (x < width && y < height)
                                c = pixels[y * width + x];
                            else
#if USE_SYSTEM_DRAWING
                                c = Color.FromArgb(0, 0, 0, 0);
#elif USE_IMAGESHARP
                                c = Color.Transparent;
#endif
                        }
                        else
                        {
                            // On 3DS we need local transposition (swapping X and Y) to work around the Z-order swizzle conflict
                            int blockX = x / 4;
                            int blockY = y / 4;
                            int localX = x % 4;
                            int localY = y % 4;

                            int sampleX = blockX * 4 + localY;
                            int sampleY = blockY * 4 + localX;

                            if (sampleX < width && sampleY < height)
                                c = pixels[sampleY * width + sampleX];
                            else
#if USE_SYSTEM_DRAWING
                                c = Color.FromArgb(0, 0, 0, 0);
#elif USE_IMAGESHARP
                                c = Color.Transparent;
#endif
                        }

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

                // Compress the image
                byte[] linearCompressed = [];
                if (isEtc1)
                {
                    linearCompressed = EtcpakTool.CompressETC1(rgbaData, paddedW, paddedH, hasAlpha);
                }
                else if (isDxt)
                {
                    linearCompressed = EtcpakTool.CompressBC(rgbaData, paddedW, paddedH, version);
                }

                if (isSwitch)
                {
                    // Block-compressed formats (BC1/BC5) for the Switch do not require additional swizzling
                    return linearCompressed;
                }

                byte[] swizzledCompressed = new byte[linearCompressed.Length];

                int blockSizeBytes = (hasAlpha || version >= 5) ? 16 : 8;
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
            if (isSwitch)
            {
                var switchSwizzle = new IMGCSwitchSwizzle(width, height);
                var points = switchSwizzle.GetPointSequence().ToArray();
                var ms = new MemoryStream();

                foreach (var point in points)
                {
                    Color color;
                    if (point.X < width && point.Y < height)
                        color = pixels[point.Y * width + point.X];
                    else
#if USE_SYSTEM_DRAWING
                        color = Color.FromArgb(0, 0, 0, 0);
#elif USE_IMAGESHARP
                        color = Color.Transparent;
#endif

                    byte[]? encoded = imgFormat.Encode(color);
                    if (encoded != null)
                        ms.Write(encoded, 0, encoded.Length);
                }

                return ms.ToArray();
            }
            else
            {
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
                        color = Color.FromArgb(0, 0, 0, 0);
#elif USE_IMAGESHARP
                        color = Color.Transparent;
#endif

                    byte[]? encoded = imgFormat.Encode(color);
                    if (encoded != null)
                        ms.Write(encoded, 0, encoded.Length);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Breaks down encoded data into 8x8 tiles, deduplicates them,
        /// and outputs the index table and the unique image buffer without duplicates.
        /// </summary>
        private void Deflate(byte[] encodedData, int bitDepth, bool isSwitch,
                                     out byte[] tileTable, out byte[] uniqueImageData)
        {
            int blockSize = 64 * bitDepth / 8;

            var tableMs = new MemoryStream();
            var imageMs = new MemoryStream();
            var uniqueBlocks = new List<byte[]>();

            var bw = new BinaryDataWriter(tableMs);

            // Writing the 8-byte magic header required by the Switch index table
            if (isSwitch)
            {
                bw.Write((ushort)0x453);
                bw.Write((ushort)0);
                bw.Write((uint)0);
            }

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
                    if (isSwitch) bw.Write((uint)existingIndex);
                    else bw.Write((ushort)existingIndex);
                }
                else
                {
                    // New block -> add it to the unique list and write index
                    if (isSwitch) bw.Write((uint)uniqueBlocks.Count);
                    else bw.Write((ushort)uniqueBlocks.Count);

                    uniqueBlocks.Add(block);
                    imageMs.Write(block, 0, blockSize);
                }
            }

            tileTable = tableMs.ToArray();
            uniqueImageData = imageMs.ToArray();
        }
    }
}