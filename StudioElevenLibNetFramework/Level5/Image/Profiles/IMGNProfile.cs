using System.IO;
using System.Linq;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Image.Swizzles;

using StudioElevenLib.Common.Imaging;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Profiles
{
    public class IMGNProfile : Image5Profile
    {
        public override string Name => "IMGN";
        public override uint Magic => 0x4E474D49u;
        public override Dictionary<byte, IPixelFormat> PixelFormats => Image5Support.ImgnPixelFormats;

        public override (int OutputWidth, int OutputHeight, IEnumerable<Point> Points) GetSwizzleSequence(int width, int height)
        {
            int outWidth = (height + 7) & ~7;
            int outHeight = (width + 7) & ~7;
            return (outWidth, outHeight, new IMGNSwizzle(width, height).GetPointSequence());
        }

        public override (int Width, int Height) GetHeaderDimensions(int width, int height, IPixelFormat format)
        {
            if (format.IsBlock) return (width, height);
            return (height, width); // Swapped back
        }

        public override void ReadTileTable(BinaryDataReader table, BinaryDataReader tex, MemoryStream ms, int bitDepth)
        {
            int tableLength = (int)table.BaseStream.Length;

            // Read magic 0x453 for Switch
            var tmp = table.ReadValue<ushort>();
            if (tmp != 0x453) throw new InvalidDataException("Switch 0x453 magic not found in table.");

            table.BaseStream.Position = 8; // skip entryStart
            var entries = new List<long>();
            for (int i = 8; i < tableLength; i += 4)
            {
                uint val = table.ReadValue<uint>();
                entries.Add(val == 0xFFFFFFFF ? -1 : val);
            }
            InflateBlocks(tex, ms, bitDepth, entries);
        }

        public override void WriteTileTable(BinaryDataWriter bw, byte[] encodedData, int bitDepth, out byte[] uniqueImageData)
        {
            bw.Write((ushort)0x453);
            bw.Write((ushort)0);
            bw.Write((uint)0);
            DeflateBlocks(encodedData, bitDepth, (index) => bw.Write((uint)index), out uniqueImageData);
        }

        public override void DecodeBlockPixels(byte[] compressedData, int width, int height, IPixelFormat format, Color[] resultArray)
        {
            int paddedW = (width + 7) & ~7;
            int paddedH = (height + 7) & ~7;

            // Unpack linear data using etcpack
            byte[] rawPic = EtcpakTool.Decompress(format, compressedData, paddedW, paddedH);

            for (int y = 0; y < paddedH; y++)
            {
                for (int x = 0; x < paddedW; x++)
                {
                    if (x < width && y < height)
                    {
                        int srcIdx = (y * paddedW + x) * 4;
                        int destIdx = y * width + x;

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
                    Color c = (x < width && y < height) ? pixels[y * width + x] : GetTransparentColor();

#if USE_SYSTEM_DRAWING
                    rgbaData[idx] = c.R; rgbaData[idx + 1] = c.G; rgbaData[idx + 2] = c.B; rgbaData[idx + 3] = c.A;
#elif USE_IMAGESHARP
                    var px = c.ToPixel<Rgba32>();
                    rgbaData[idx] = px.R; rgbaData[idx + 1] = px.G; rgbaData[idx + 2] = px.B; rgbaData[idx + 3] = px.A;
#endif
                }
            }
            return EtcpakTool.Compress(format, rgbaData, paddedW, paddedH);
        }
    }
}
