#nullable enable

using System;
using System.IO;
using System.Linq;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using System.Runtime.InteropServices;

#if USE_SYSTEM_DRAWING
using System.Drawing;
using System.Drawing.Imaging;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.IMGC
{
    public class IMGCReader
    {
        private readonly Stream _baseStream;

        public IMGCReader(Stream stream)
        {
            _baseStream = stream;
        }

#if USE_SYSTEM_DRAWING
        public (IMGCSupport.Header header, Bitmap bitmap, Color[] pixels, int width, int height, IPixelFormat imageFormat) Read()
#elif USE_IMAGESHARP
        public (IMGCSupport.Header header, Image<Rgba32> bitmap, Color[] pixels, int width, int height, IPixelFormat imageFormat) Read()
#endif
        {
            BinaryDataReader data = new BinaryDataReader(_baseStream);

            var header = data.ReadStruct<IMGCSupport.Header>();

            byte[] tileData = Compressor.Decompress(data.GetSection((uint)header.TileOffset, header.TileSize1));
            byte[] imageData = Compressor.Decompress(data.GetSection((uint)(header.TileOffset + header.TileSize2), header.ImageSize));

            var imageFormat = IMGCSupport.ImageFormats[header.ImageFormat];
            var decoded = DecodeImage(tileData, imageData, imageFormat, header.Width, header.Height, header.BitDepth);

            return (header, decoded.bitmap, decoded.pixels, header.Width, header.Height, imageFormat);
        }

#if USE_SYSTEM_DRAWING
        private (Bitmap bitmap, Color[] pixels) DecodeImage(byte[] tile, byte[] imageData, IPixelFormat imgFormat, int width, int height, int bitDepth)
#elif USE_IMAGESHARP
        private (Image<Rgba32> bitmap, Color[] pixels) DecodeImage(byte[] tile, byte[] imageData, IPixelFormat imgFormat, int width, int height, int bitDepth)
#endif
        {
            byte[]? entryStart = null;

            using (var table = new BinaryDataReader(tile))
            using (var tex = new BinaryDataReader(imageData))
            {
                int tableLength = (int)table.BaseStream.Length;

                var tmp = table.ReadValue<ushort>();
                table.BaseStream.Position = 0;
                var entryLength = 2;
                if (tmp == 0x453)
                {
                    entryStart = table.ReadMultipleValue<byte>(8);
                    entryLength = 4;
                }

                var ms = new MemoryStream();
                for (int i = (int)table.BaseStream.Position; i < tableLength; i += entryLength)
                {
                    uint entry = (entryLength == 2) ? table.ReadValue<ushort>() : table.ReadValue<uint>();
                    if (entry == 0xFFFF || entry == 0xFFFFFFFF)
                    {
                        for (int j = 0; j < 64 * bitDepth / 8; j++) ms.WriteByte(0);
                    }
                    else
                    {
                        if (entry * (64 * bitDepth / 8) < tex.BaseStream.Length)
                        {
                            tex.BaseStream.Position = entry * (64 * bitDepth / 8);
                            for (int j = 0; j < 64 * bitDepth / 8; j++) ms.WriteByte(tex.ReadValue<byte>());
                        }
                    }
                }

                int paddedW = (width + 7) & ~7;
                int paddedH = (height + 7) & ~7;
                int paddedCount = paddedW * paddedH;
                Color[] resultArray = new Color[width * height];

                bool isEtc1 = imgFormat.Name == "ETC1";
                bool isEtc1a4 = imgFormat.Name == "ETC1A4";
                bool isEtc = isEtc1 || isEtc1a4;

                if (isEtc)
                {
                    paddedW = (width + 7) & ~7;
                    paddedH = (height + 7) & ~7;
                    int blockSizeBytes = isEtc1a4 ? 16 : 8;
                    int blocksX = paddedW / 4;

                    byte[] swizzledCompressed = ms.ToArray();
                    byte[] linearCompressed = new byte[swizzledCompressed.Length];

                    IMGCSwizzle swizzle = new IMGCSwizzle(width, height);
                    var pointsArr = swizzle.GetPointSequence().ToArray();

                    // Unswizzle the blocks (Reorder for etcpak)
                    int numBlocks = swizzledCompressed.Length / blockSizeBytes;
                    for (int i = 0; i < numBlocks; i++)
                    {
                        if (i * 16 >= pointsArr.Length) break;

                        var pt = pointsArr[i * 16];
                        int blockX = pt.X / 4;
                        int blockY = pt.Y / 4;

                        int linearBlockIndex = blockY * blocksX + blockX;

                        if (linearBlockIndex * blockSizeBytes < linearCompressed.Length && i * blockSizeBytes < swizzledCompressed.Length)
                        {
                            Array.Copy(swizzledCompressed, i * blockSizeBytes, linearCompressed, linearBlockIndex * blockSizeBytes, blockSizeBytes);
                        }
                    }

                    // Unpack linear data using etcpack
                    byte[] rawPic = EtcpakTool.Decompress(linearCompressed, paddedW, paddedH, isEtc1a4);

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
                else
                {
                    // Other pixel format
                    byte[] pic = ms.ToArray();
                    IMGCSwizzle imgcSwizzle = new IMGCSwizzle(width, height);
                    var points = imgcSwizzle.GetPointSequence().ToArray();

                    for (int i = 0; i < paddedCount; i++)
                    {
                        int dataIndex = i * imgFormat.Size;
                        byte[] group = new byte[imgFormat.Size];
                        Array.Copy(pic, dataIndex, group, 0, imgFormat.Size);

                        var pt = points[i];
                        if (pt.X < width && pt.Y < height)
                            resultArray[pt.Y * width + pt.X] = imgFormat.Decode(group);
                    }
                }

#if USE_SYSTEM_DRAWING
                var bmp = new Bitmap(width, height);
                var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int pixelOffset = data.Stride * y / 4 + x;
                        int pixelValue = resultArray[y * width + x].ToArgb();
                        Marshal.WriteInt32(data.Scan0 + pixelOffset * 4, pixelValue);
                    }
                }
                bmp.UnlockBits(data);
                return (bmp, resultArray);
#elif USE_IMAGESHARP
                var bmp = new Image<Rgba32>(width, height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        bmp[x, y] = resultArray[y * width + x].ToPixel<Rgba32>();
                    }
                }
                return (bmp, resultArray);
#endif
            }
        }
    }
}
