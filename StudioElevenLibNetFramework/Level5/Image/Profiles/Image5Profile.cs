#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using StudioElevenLib.Tools;
using StudioElevenLib.Common.Imaging;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Profiles
{
    public abstract class Image5Profile
    {
        public abstract string Name { get; }
        public abstract uint Magic { get; }
        public abstract Dictionary<byte, IPixelFormat> PixelFormats { get; }

        public abstract (int OutputWidth, int OutputHeight, IEnumerable<Point> Points) GetSwizzleSequence(int width, int height);
        public abstract (int Width, int Height) GetHeaderDimensions(int width, int height, IPixelFormat format);

        public abstract void ReadTileTable(BinaryDataReader table, BinaryDataReader tex, MemoryStream ms, int bitDepth);
        public abstract void WriteTileTable(BinaryDataWriter bw, byte[] encodedData, int bitDepth, out byte[] uniqueImageData);

        public abstract void DecodeBlockPixels(byte[] compressedData, int width, int height, IPixelFormat format, Color[] resultArray);
        public abstract byte[] EncodeBlockPixels(Color[] pixels, int width, int height, IPixelFormat format);

        protected void InflateBlocks(BinaryDataReader tex, MemoryStream ms, int bitDepth, IEnumerable<long> entries)
        {
            int blockSize = 64 * bitDepth / 8;
            long texLength = tex.BaseStream.Length;
            foreach (var entry in entries)
            {
                if (entry == -1)
                {
                    for (int j = 0; j < blockSize; j++) ms.WriteByte(0);
                }
                else
                {
                    long offset = entry * blockSize;
                    if (offset < texLength)
                    {
                        tex.BaseStream.Position = offset;
                        for (int j = 0; j < blockSize; j++) ms.WriteByte(tex.ReadValue<byte>());
                    }
                    else
                    {
                        for (int j = 0; j < blockSize; j++) ms.WriteByte(0);
                    }
                }
            }
        }

        protected void DeflateBlocks(byte[] encodedData, int bitDepth, Action<int> writeIndex, out byte[] uniqueImageData)
        {
            int blockSize = 64 * bitDepth / 8;
            var imageMs = new MemoryStream();
            var uniqueBlocks = new List<byte[]>();

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
                    writeIndex(existingIndex);
                }
                else
                {
                    // New block -> add it to the unique list and write index
                    writeIndex(uniqueBlocks.Count);
                    uniqueBlocks.Add(block);
                    imageMs.Write(block, 0, copyLen);
                }
            }
            uniqueImageData = imageMs.ToArray();
        }

        protected Color GetTransparentColor()
        {
#if USE_SYSTEM_DRAWING
            return Color.FromArgb(0, 0, 0, 0);
#elif USE_IMAGESHARP
            return Color.Transparent;
#else
            return default;
#endif
        }
    }
}
