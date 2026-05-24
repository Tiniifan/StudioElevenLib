using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using StudioElevenLib.Level5.Compression;

#if USE_SYSTEM_DRAWING
using System.Drawing;
using System.Drawing.Imaging;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Tools
{
    /// <summary>
    /// Wraps the etcpak native binary to compress/decompress ETC1 / ETC1A4 texture data.
    /// </summary>
    public static class EtcpakTool
    {
        /// <summary>
        /// Resolved path to the etcpak binary for the current OS.
        /// </summary>
        public static string EtcpakPath { get; } = ResolveEtcpakPath();

        private static string ResolveEtcpakPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("etcpak is not available for macOS.");

            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                             ?? AppContext.BaseDirectory;

            // Determine the correct executable based on the operating system
            string relativePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine("bin", "win-x64", "etcpak.exe")
                : Path.Combine("bin", "linux-x64", "etcpak");

            string fullPath = Path.Combine(baseDir, relativePath);

            // Restore executable permissions on Linux
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(fullPath))
            {
                try
                {
                    using (var proc = Process.Start(new ProcessStartInfo("chmod", $"+x \"{fullPath}\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }))
                    {
                        proc?.WaitForExit();
                    }
                }
                catch
                {
                    /* ignored */
                }
            }

            return fullPath;
        }

        private const ulong PVR_ETC1 = 6;
        private const ulong PVR_ETC2_RGBA = 23;

        /// <summary>
        /// Decompresses raw ETC data into linear RGBA8.
        /// </summary>
        public static byte[] Decompress(byte[] etcData, int width, int height, bool hasAlpha)
        {
            string tmpPvr = Path.ChangeExtension(Path.GetTempFileName(), ".pvr");
            string tmpPng = Path.ChangeExtension(Path.GetTempFileName(), ".png");

            try
            {
                byte[] colorData;
                byte[] alphaData = new byte[0];

                // Separate alpha and color blocks if the texture has an alpha channel
                if (hasAlpha)
                {
                    int numBlocks = etcData.Length / 16;
                    colorData = new byte[numBlocks * 8];
                    alphaData = new byte[numBlocks * 8];

                    for (int i = 0; i < numBlocks; i++)
                    {
                        // Each 16-byte block contains 8 bytes of alpha followed by 8 bytes of color
                        Array.Copy(etcData, i * 16, alphaData, i * 8, 8);
                        Array.Copy(etcData, i * 16 + 8, colorData, i * 8, 8);
                    }
                }
                else
                {
                    colorData = (byte[])etcData.Clone();
                }

                // Endianness swap for the color data blocks
                for (int i = 0; i + 8 <= colorData.Length; i += 8)
                {
                    Array.Reverse(colorData, i, 8);
                }

                // Write data to a temporary PVR container for etcpak to process
                WritePvr(tmpPvr, colorData, width, height, PVR_ETC1);
                RunEtcpak($"-v \"{tmpPvr}\" \"{tmpPng}\"");

                byte[] rgbaResult = ReadPngAsRgba(tmpPng, width, height);

                if (!hasAlpha)
                {
                    return rgbaResult;
                }

                // Reapply alpha values to the decoded RGB image
                int numBlocksX = (width + 3) / 4;
                int numBlocksY = (height + 3) / 4;

                for (int blockY = 0; blockY < numBlocksY; blockY++)
                {
                    for (int blockX = 0; blockX < numBlocksX; blockX++)
                    {
                        int blockIdx = blockY * numBlocksX + blockX;
                        ulong alphaBlock = BitConverter.ToUInt64(alphaData, blockIdx * 8);

                        for (int col = 0; col < 4; col++)
                        {
                            for (int row = 0; row < 4; row++)
                            {
                                int pixX = blockX * 4 + col;
                                int pixY = blockY * 4 + row;

                                if (pixX >= width || pixY >= height)
                                    continue;

                                // Extract 4-bit alpha value and scale it to 8-bit
                                byte a4 = (byte)((alphaBlock >> (4 * (col * 4 + row))) & 0xF);

                                rgbaResult[(pixY * width + pixX) * 4 + 3] = (byte)(a4 * 17);
                            }
                        }
                    }
                }

                return rgbaResult;
            }
            finally
            {
                TryDelete(tmpPvr);
                TryDelete(tmpPng);
            }
        }

        /// <summary>
        /// Compresses linear RGBA8 data into raw ETC1 / ETC1A4.
        /// </summary>
        public static byte[] Compress(byte[] rgbaData, int width, int height, bool hasAlpha)
        {
            string tmpPng = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            string tmpPvr = Path.ChangeExtension(Path.GetTempFileName(), ".pvr");

            try
            {
                WriteRgbaAsPng(rgbaData, width, height, tmpPng);
                RunEtcpak($"-c etc1 -d --disable-heuristics \"{tmpPng}\" \"{tmpPvr}\"");
                byte[] colorData = ReadPvrPayload(tmpPvr);

                // Endianness swap for the compressed color data blocks
                for (int i = 0; i + 8 <= colorData.Length; i += 8)
                {
                    Array.Reverse(colorData, i, 8);
                }

                if (!hasAlpha)
                {
                    return colorData;
                }

                int numBlocksX = (width + 3) / 4;
                int numBlocksY = (height + 3) / 4;
                byte[] result = new byte[numBlocksX * numBlocksY * 16];
                int resultOff = 0;
                int colorOff = 0;

                // Extract and compress the alpha channel into 4-bit values
                for (int blockY = 0; blockY < numBlocksY; blockY++)
                {
                    for (int blockX = 0; blockX < numBlocksX; blockX++)
                    {
                        ulong alphaBlock = 0;
                        for (int col = 0; col < 4; col++)
                        {
                            for (int row = 0; row < 4; row++)
                            {
                                int pixX = blockX * 4 + col;
                                int pixY = blockY * 4 + row;
                                byte a8 = (pixX < width && pixY < height)
                                            ? rgbaData[(pixY * width + pixX) * 4 + 3]
                                            : (byte)0;

                                // Scale 8-bit alpha down to 4-bit
                                byte a4 = (byte)Math.Round(a8 / 17.0);
                                alphaBlock |= ((ulong)a4) << (4 * (col * 4 + row));
                            }
                        }

                        // Combine alpha block and color block into the final compressed payload
                        Array.Copy(BitConverter.GetBytes(alphaBlock), 0, result, resultOff, 8);
                        resultOff += 8;
                        Array.Copy(colorData, colorOff, result, resultOff, 8);
                        colorOff += 8;
                        resultOff += 8;
                    }
                }

                return result;
            }
            finally
            {
                TryDelete(tmpPng);
                TryDelete(tmpPvr);
            }
        }

        private static void WritePvr(string path, byte[] data, int width, int height, ulong pixelFormat)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            {
                using (var bw = new BinaryDataWriter(fs))
                {
                    // Write standard PVR3 header
                    bw.Write(0x03525650u); // PVR magic number
                    bw.Write(0u);          // Flags
                    bw.Write(pixelFormat); // Pixel format
                    bw.Write(0u);          // Color space
                    bw.Write(0u);          // Channel type
                    bw.Write((uint)height);
                    bw.Write((uint)width);
                    bw.Write(1u);          // Depth
                    bw.Write(1u);          // Num surfaces
                    bw.Write(1u);          // Num faces
                    bw.Write(1u);          // Mipmap count
                    bw.Write(0u);          // Metadata size
                    bw.Write(data);        // Texture payload
                }
            }
        }

        private static byte[] ReadPvrPayload(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            // Read metadata size from the PVR header (offset 48)
            uint metaSize = BitConverter.ToUInt32(bytes, 48);

            // Calculate payload start offset (52 bytes header + metadata size)
            int start = 52 + (int)metaSize;
            byte[] result = new byte[bytes.Length - start];
            Array.Copy(bytes, start, result, 0, result.Length);

            return result;
        }

        private static byte[] ReadPngAsRgba(string pngPath, int width, int height)
        {
            var result = new byte[width * height * 4];
#if USE_SYSTEM_DRAWING
            using (var bmp = new Bitmap(pngPath))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var c = bmp.GetPixel(x, y);
                        int i = (y * width + x) * 4;
                        result[i] = c.R; result[i + 1] = c.G; result[i + 2] = c.B; result[i + 3] = c.A;
                    }
                }
            }
#elif USE_IMAGESHARP
            using (var img = Image.Load<Rgba32>(pngPath))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width;  x++)
                    {
                        var p = img[x, y];
                        int i = (y * width + x) * 4;
                        result[i] = p.R; result[i+1] = p.G; result[i+2] = p.B; result[i+3] = p.A;
                    }
                }
            }
#endif
            return result;
        }

        private static void WriteRgbaAsPng(byte[] rgba, int width, int height, string pngPath)
        {
#if USE_SYSTEM_DRAWING
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int i = (y * width + x) * 4;
                        bmp.SetPixel(x, y, Color.FromArgb(rgba[i + 3], rgba[i], rgba[i + 1], rgba[i + 2]));
                    }
                }
                bmp.Save(pngPath, ImageFormat.Png);
            }
#elif USE_IMAGESHARP
            using (var img = new Image<Rgba32>(width, height))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width;  x++)
                    {
                        int i = (y * width + x) * 4;
                        img[x, y] = new Rgba32(rgba[i], rgba[i+1], rgba[i+2], rgba[i+3]);
                    }
                }
                img.SaveAsPng(pngPath);
            }
#endif
        }

        private static void RunEtcpak(string args)
        {
            args = args.Replace('\\', '/');

            var psi = new ProcessStartInfo(EtcpakPath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                if (proc == null)
                {
                    throw new InvalidOperationException("Failed to start etcpak");
                }

                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    throw new Exception(
                        $"etcpak failed (code {proc.ExitCode})\nargs: {args}\nstdout: {stdout}\nstderr: {stderr}");
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                /* ignored */
            }
        }
    }
}