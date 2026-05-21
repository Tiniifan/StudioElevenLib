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
        public (IMGCSupport.Header header, Bitmap bitmap, Color[] pixels, int width, int height, IColorFormat imageFormat) Read()
#elif USE_IMAGESHARP
        public (IMGCSupport.Header header, Image<Rgba32> bitmap, Color[] pixels, int width, int height, IColorFormat imageFormat) Read()
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
        private (Bitmap bitmap, Color[] pixels) DecodeImage(byte[] tile, byte[] imageData, IColorFormat imgFormat, int width, int height, int bitDepth)
#elif USE_IMAGESHARP
        private (Image<Rgba32> bitmap, Color[] pixels) DecodeImage(byte[] tile, byte[] imageData, IColorFormat imgFormat, int width, int height, int bitDepth)
#endif
        {
            byte[] entryStart = null;

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
                        for (int j = 0; j < 64 * bitDepth / 8; j++)
                        {
                            ms.WriteByte(0);
                        }
                    }
                    else
                    {
                        if (entry * (64 * bitDepth / 8) < tex.BaseStream.Length)
                        {
                            tex.BaseStream.Position = entry * (64 * bitDepth / 8);
                            for (int j = 0; j < 64 * bitDepth / 8; j++)
                            {
                                ms.WriteByte(tex.ReadValue<byte>());
                            }
                        }
                    }
                }

                byte[] pic;
                switch (imgFormat.Name)
                {
                    case "ETC1A4":
                        pic = new Compression.ETC1.ETC1(true, width, height).Decompress(ms.ToArray());
                        break;
                    case "ETC1":
                        pic = new Compression.ETC1.ETC1(false, width, height).Decompress(ms.ToArray());
                        break;
                    default:
                        pic = ms.ToArray();
                        break;
                }

                IMGCSwizzle imgcSwizzle = new IMGCSwizzle(width, height);
                var points = imgcSwizzle.GetPointSequence();

                int pixelCount = width * height;
                Color[] resultArray = new Color[pixelCount];

                for (int i = 0; i < pixelCount; i++)
                {
                    int dataIndex = i * imgFormat.Size;
                    byte[] group = new byte[imgFormat.Size];
                    Array.Copy(pic, dataIndex, group, 0, imgFormat.Size);
                    resultArray[i] = imgFormat.Decode(group);
                }

#if USE_SYSTEM_DRAWING
                var bmp = new Bitmap(width, height);
                var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                foreach (var pair in points.Zip(resultArray, Tuple.Create))
                {
                    int x = pair.Item1.X, y = pair.Item1.Y;
                    if (0 <= x && x < width && 0 <= y && y < height)
                    {
                        var color = pair.Item2;
                        int pixelOffset = data.Stride * y / 4 + x;
                        int pixelValue = color.ToArgb();
                        Marshal.WriteInt32(data.Scan0 + pixelOffset * 4, pixelValue);
                    }
                }

                bmp.UnlockBits(data);

                return (bmp, resultArray);
#elif USE_IMAGESHARP
                var bmp = new Image<Rgba32>(width, height);

                foreach (var pair in points.Zip(resultArray, Tuple.Create))
                {
                    int x = pair.Item1.X, y = pair.Item1.Y;
                    if (0 <= x && x < width && 0 <= y && y < height)
                    {
                        var color = pair.Item2;
                        bmp[x, y] = color.ToPixel<Rgba32>();
                    }
                }

                return (bmp, resultArray);
#endif
            }
        }
    }
}
