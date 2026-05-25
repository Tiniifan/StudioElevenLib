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

            bool isSwitchFile = false;
            if (tileData.Length >= 2)
            {
                isSwitchFile = BitConverter.ToUInt16(tileData, 0) == 0x453;
            }

            Console.WriteLine(isSwitchFile.ToString() + " " + header.ImageFormat.ToString("X2"));

            var imageFormat = isSwitchFile ? IMGCSupport.SwitchPixelFormats[header.ImageFormat] : IMGCSupport.PixelFormats3DS[header.ImageFormat];
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
            bool isSwitchFile = false; 

            using (var table = new BinaryDataReader(tile))
            using (var tex = new BinaryDataReader(imageData))
            {
                int tableLength = (int)table.BaseStream.Length;

                var tmp = table.ReadValue<ushort>();
                table.BaseStream.Position = 0;
                var entryLength = 2;

                // Switch Format
                if (tmp == 0x453)
                {
                    entryStart = table.ReadMultipleValue<byte>(8);
                    entryLength = 4;
                    isSwitchFile = true;
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

                bool isEtc1 = imgFormat.Name == "ETC1" || imgFormat.Name == "ETC1A4";
                bool hasAlpha = imgFormat.Name == "ETC1A4";

                bool isBc = imgFormat.Name == "BC1" || imgFormat.Name == "BC5";
                int version = imgFormat.Name == "BC1" ? 1 : imgFormat.Name == "BC5" ? 5 : 0;

                bool isBlockFormat = isEtc1 || isBc;

                if (isBlockFormat)
                {
                    paddedW = (width + 7) & ~7;
                    paddedH = (height + 7) & ~7;
                    int blockSizeBytes = (hasAlpha || version >= 5) ? 16 : 8;
                    int blocksX = paddedW / 4;

                    byte[] swizzledCompressed = ms.ToArray();
                    byte[] linearCompressed = new byte[swizzledCompressed.Length];

                    if (isBc)
                    {
                        // The blocks are already in linear order
                        linearCompressed = swizzledCompressed;
                    }
                    else
                    {
                        // Z-order swizzle is required on 3ds
                        linearCompressed = new byte[swizzledCompressed.Length];
                        IMGCSwizzle swizzle = new IMGCSwizzle(width, height);
                        var pointsArr = swizzle.GetPointSequence().ToArray();

                        int numBlocks = swizzledCompressed.Length / blockSizeBytes;
                        for (int i = 0; i < numBlocks; i++)
                        {
                            if (i * 16 >= pointsArr.Length) break;
                            var pt = pointsArr[i * 16];
                            int blockX = pt.X / 4;
                            int blockY = pt.Y / 4;
                            int linearBlockIndex = blockY * blocksX + blockX;

                            if (linearBlockIndex * blockSizeBytes < linearCompressed.Length
                                && i * blockSizeBytes < swizzledCompressed.Length)
                            {
                                Array.Copy(swizzledCompressed, i * blockSizeBytes,
                                           linearCompressed, linearBlockIndex * blockSizeBytes,
                                           blockSizeBytes);
                            }
                        }
                    }

                    // Unpack linear data using etcpack
                    byte[] rawPic = [];
                    if (isEtc1)
                        rawPic = EtcpakTool.DecompressETC1(linearCompressed, paddedW, paddedH, hasAlpha);
                    else
                        rawPic = EtcpakTool.DecompressBC(linearCompressed, paddedW, paddedH, version);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
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
                else
                {
                    // Other pixel format
                    byte[] pic = ms.ToArray();

                    if (isSwitchFile)
                    {
                        // Switch : layout column-major, pas de z-order
                        int tileBytes = 64 * imgFormat.Size;
                        int numTiles = pic.Length / tileBytes;
                        int tileX = 0, tileY = 0;

                        for (int i = 0; i < numTiles; i++)
                        {
                            for (int h = 0; h < 64; h++)
                            {
                                int x1 = h / 8; // column in the 8×8 tile
                                int y1 = h % 8; // line in the 8×8 tile
                                int px = tileX + x1;
                                int py = tileY + y1;

                                if (px < width && py < height)
                                {
                                    int srcOffset = i * tileBytes + h * imgFormat.Size;
                                    byte[] group = new byte[imgFormat.Size];
                                    Array.Copy(pic, srcOffset, group, 0, imgFormat.Size);
                                    resultArray[py * width + px] = imgFormat.Decode(group);
                                }
                            }

                            // Tiles: column by column (y first, then x)
                            tileY += 8;
                            if (tileY >= height)
                            {
                                tileY = 0;
                                tileX += 8;
                            }
                        }


                    }
                    else
                    {
                        // On 3DS we have to do Z-order swizzle
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
                }


                if (isSwitchFile && !isBlockFormat)
                {
                    byte[] pic = ms.ToArray();
                    var switchSwizzle = new IMGCSwitchSwizzle(width, height);
                    var points = switchSwizzle.GetPointSequence().ToArray();

                    // The output dimensions are transposed
                    int outWidth = switchSwizzle.Width;
                    int outHeight = switchSwizzle.Height;
                    Color[] resultArraySwitch = new Color[outWidth * outHeight];

                    int i = 0;
                    foreach (var pt in points)
                    {
                        if (i * imgFormat.Size + imgFormat.Size > pic.Length) break;

                        byte[] group = new byte[imgFormat.Size];
                        Array.Copy(pic, i * imgFormat.Size, group, 0, imgFormat.Size);

                        if (pt.X < outWidth && pt.Y < outHeight)
                            resultArraySwitch[pt.Y * outWidth + pt.X] = imgFormat.Decode(group);

                        i++;
                    }

                    resultArray = resultArraySwitch;
                    width = outWidth;
                    height = outHeight;
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