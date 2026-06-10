#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using StudioElevenLib.Tools;
using StudioElevenLib.Common.Imaging;
using StudioElevenLib.Level5.Compression;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.IO
{
    internal class Image5Reader
    {
        private readonly Stream _baseStream;
        private readonly Image5 _image;

        public Image5Reader(Stream stream, Image5 image)
        {
            _baseStream = stream;
            _image = image;
        }

        public void Read()
        {
            var methodName = $"OpenV{_image.Version}";
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
                throw new NotSupportedException($"{_image.Profile.Name} version {_image.Version} is not supported. Method {methodName} not found.");

            BinaryDataReader data = new BinaryDataReader(_baseStream);

            // Invoke the version-specific open method via Reflection
            method.Invoke(this, new object[] { data });
        }

        private void OpenV0(BinaryDataReader data)
        {
            var header = data.ReadStruct<Image5Support.Header>();

            byte[] tileData = Compressor.Decompress(data.GetSection((uint)header.TileOffset, header.TileSize1));
            byte[] imageData = Compressor.Decompress(data.GetSection((uint)(header.TileOffset + header.TileSize2), header.ImageSize));

            var imageFormat = _image.Profile.PixelFormats[header.ImageFormat];
            _image.ImageFormat = imageFormat;

            DecodeImage(tileData, imageData, imageFormat, header.Width, header.Height, header.BitDepth);
        }

        private void DecodeImage(byte[] tile, byte[] imageData, IPixelFormat imgFormat, int width, int height, int bitDepth)
        {
            using (var table = new BinaryDataReader(tile))
            using (var tex = new BinaryDataReader(imageData))
            {
                var ms = new MemoryStream();
                _image.Profile.ReadTileTable(table, tex, ms, bitDepth);

                Color[] resultArray;
                int outWidth = width;
                int outHeight = height;

                if (imgFormat.IsBlock)
                {
                    resultArray = new Color[width * height];
                    _image.Profile.DecodeBlockPixels(ms.ToArray(), width, height, imgFormat, resultArray);
                }
                else
                {
                    byte[] pic = ms.ToArray();
                    var swizzleData = _image.Profile.GetSwizzleSequence(width, height);
                    var points = swizzleData.Points.ToArray();

                    outWidth = swizzleData.OutputWidth;
                    outHeight = swizzleData.OutputHeight;
                    resultArray = new Color[outWidth * outHeight];

                    int i = 0;
                    foreach (var pt in points)
                    {
                        int dataIndex = i * imgFormat.Size;
                        if (dataIndex + imgFormat.Size > pic.Length) break;

                        byte[] group = new byte[imgFormat.Size];
                        Array.Copy(pic, dataIndex, group, 0, imgFormat.Size);

                        if (pt.X < outWidth && pt.Y < outHeight)
                            resultArray[pt.Y * outWidth + pt.X] = imgFormat.Decode(group);

                        i++;
                    }
                }

                _image.Width = outWidth;
                _image.Height = outHeight;
                _image.Pixels = resultArray;

#if USE_SYSTEM_DRAWING
                var bmp = new Bitmap(outWidth, outHeight);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, outWidth, outHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                for (int y = 0; y < outHeight; y++)
                {
                    for (int x = 0; x < outWidth; x++)
                    {
                        int pixelOffset = bmpData.Stride * y / 4 + x;
                        int pixelValue = resultArray[y * outWidth + x].ToArgb();
                        Marshal.WriteInt32(bmpData.Scan0 + pixelOffset * 4, pixelValue);
                    }
                }
                bmp.UnlockBits(bmpData);
                _image.Bitmap = bmp;
#elif USE_IMAGESHARP
                var bmp = new SixLabors.ImageSharp.Image<Rgba32>(outWidth, outHeight);
                for (int y = 0; y < outHeight; y++)
                {
                    for (int x = 0; x < outWidth; x++)
                    {
                        bmp[x, y] = resultArray[y * outWidth + x].ToPixel<Rgba32>();
                    }
                }
                _image.Bitmap = bmp;
#endif
            }
        }
    }
}
