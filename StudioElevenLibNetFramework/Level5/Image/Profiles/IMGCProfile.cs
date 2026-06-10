using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using StudioElevenLib.Tools;
using StudioElevenLib.Common.Imaging;
using StudioElevenLib.Level5.Image.Swizzles;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Profiles
{
    public class IMGCProfile : Image5Profile
    {
        public override string Name => "IMGC";
        public override uint Magic => 0x43474D49u;
        public override Dictionary<byte, IPixelFormat> PixelFormats => Image5Support.ImgcPixelFormats;

        public override (int OutputWidth, int OutputHeight, IEnumerable<Point> Points) GetSwizzleSequence(int width, int height)
        {
            return (width, height, new IMGCSwizzle(width, height).GetPointSequence());
        }

        public override (int Width, int Height) GetHeaderDimensions(int width, int height, IPixelFormat format)
        {
            return (width, height);
        }

        public override void ReadTileTable(BinaryDataReader table, BinaryDataReader tex, MemoryStream ms, int bitDepth)
        {
            int tableLength = (int)table.BaseStream.Length;
            var entries = new List<long>();
            for (int i = 0; i < tableLength; i += 2)
            {
                ushort val = table.ReadValue<ushort>();
                entries.Add(val == 0xFFFF ? -1 : val);
            }
            InflateBlocks(tex, ms, bitDepth, entries);
        }

        public override void WriteTileTable(BinaryDataWriter bw, byte[] encodedData, int bitDepth, out byte[] uniqueImageData)
        {
            DeflateBlocks(encodedData, bitDepth, (index) => bw.Write((ushort)index), out uniqueImageData);
        }

        public override void DecodeBlockPixels(byte[] compressedData, int width, int height, IPixelFormat format, Color[] resultArray)
        {
            int paddedW = (width + 7) & ~7;
            int paddedH = (height + 7) & ~7;
            int blockSizeBytes = format.Size;
            int blocksX = paddedW / 4;

            byte[] linearCompressed = new byte[compressedData.Length];
            var swizzle = new IMGCSwizzle(width, height);
            var pointsArr = swizzle.GetPointSequence().ToArray();

            // Unswizzle the blocks (Reorder for etcpak)
            int numBlocks = compressedData.Length / blockSizeBytes;
            for (int i = 0; i < numBlocks; i++)
            {
                if (i * 16 >= pointsArr.Length) break;
                var pt = pointsArr[i * 16];
                int blockX = pt.X / 4;
                int blockY = pt.Y / 4;
                int linearBlockIndex = blockY * blocksX + blockX;

                if (linearBlockIndex * blockSizeBytes < linearCompressed.Length && i * blockSizeBytes < compressedData.Length)
                {
                    Array.Copy(compressedData, i * blockSizeBytes, linearCompressed, linearBlockIndex * blockSizeBytes, blockSizeBytes);
                }
            }

            // Unpack linear data using etcpack
            byte[] rawPic = EtcpakTool.Decompress(format, linearCompressed, paddedW, paddedH);

            // Un-transpose the pixels
            for (int y = 0; y < paddedH; y++)
            {
                for (int x = 0; x < paddedW; x++)
                {
                    int blockX = x / 4;
                    int blockY = y / 4;
                    int localX = x % 4;
                    int localY = y % 4;

                    int targetX = blockX * 4 + localY;
                    int targetY = blockY * 4 + localX;

                    if (targetX < width && targetY < height)
                    {
                        int srcIdx = (y * paddedW + x) * 4;
                        int destIdx = targetY * width + targetX;

                        byte r = rawPic[srcIdx];
                        byte g = rawPic[srcIdx + 1];
                        byte b = rawPic[srcIdx + 2];
                        byte a = rawPic[srcIdx + 3];

#if USE_SYSTEM_DRAWING
                        resultArray[destIdx] = Color.FromArgb(a, r, g, b);
#elif USE_IMAGESHARP
                        resultArray[destIdx] = Color.FromRgba(r, g, b, a);
#endif
                    }
                }
            }
        }

        public override byte[] EncodeBlockPixels(Color[] pixels, int width, int height, IPixelFormat format)
        {
            int paddedW = (width + 7) & ~7;
            int paddedH = (height + 7) & ~7;
            byte[] rgbaData = new byte[paddedW * paddedH * 4];

            for (int y = 0; y < paddedH; y++)
            {
                for (int x = 0; x < paddedW; x++)
                {
                    int idx = (y * paddedW + x) * 4;
                    int blockX = x / 4, blockY = y / 4;
                    int localX = x % 4, localY = y % 4;
                    int sampleX = blockX * 4 + localY;
                    int sampleY = blockY * 4 + localX;

                    Color c = (sampleX < width && sampleY < height) ? pixels[sampleY * width + sampleX] : GetTransparentColor();

#if USE_SYSTEM_DRAWING
                    rgbaData[idx] = c.R; rgbaData[idx + 1] = c.G; rgbaData[idx + 2] = c.B; rgbaData[idx + 3] = c.A;
#elif USE_IMAGESHARP
                    var px = c.ToPixel<Rgba32>();
                    rgbaData[idx] = px.R; rgbaData[idx + 1] = px.G; rgbaData[idx + 2] = px.B; rgbaData[idx + 3] = px.A;
#endif
                }
            }

            // Compress the image
            byte[] linearCompressed = EtcpakTool.Compress(format, rgbaData, paddedW, paddedH);
            byte[] swizzledCompressed = new byte[linearCompressed.Length];

            int blockSizeBytes = format.Size;
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

                Array.Copy(linearCompressed, linearBlockIndex * blockSizeBytes, swizzledCompressed, i * blockSizeBytes, blockSizeBytes);
            }
            return swizzledCompressed;
        }
    }
}
