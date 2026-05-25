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
    /// Wraps the etcpak native binary to compress/decompress ETC1 / ETC1A4 / DXT texture data.
    /// </summary>
    public static class EtcpakTool
    {
        #region Constants

        private const ulong PVR_ETC1 = 6;
        private const ulong PVR_ETC2_RGBA = 23;

        // PVR3 Pixel format constants for DXT/BCn formats
        private const ulong PVR_BC1 = 7;  // BC1 / DXT1
        private const ulong PVR_BC3 = 11; // BC3 / DXT5
        private const ulong PVR_BC4 = 12; // BC4
        private const ulong PVR_BC5 = 13; // BC5
        private const ulong PVR_BC7 = 15; // BC7

        #endregion

        #region Platform Initialization

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

        #endregion

        #region ETC1 Compression

        /// <summary>
        /// Decompresses raw ETC data into linear RGBA8.
        /// </summary>
        public static byte[] DecompressETC1(byte[] etcData, int width, int height, bool hasAlpha)
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
        public static byte[] CompressETC1(byte[] rgbaData, int width, int height, bool hasAlpha)
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

        #endregion

        #region DXT Compression

        /// <summary>
        /// Decompresses raw BC data into linear RGBA8.
        /// </summary>
        /// <summary>
        public static byte[] DecompressBC(byte[] dxtData, int width, int height, int version)
        {
            if (version == 1)
            {
                return DecompressBC1(dxtData, width, height);
            }
            else if (version == 5)
            {
                return DecompressBC5(dxtData, width, height);
            }
            else
            {
                throw new NotSupportedException($"Decompression for BC{version} is not implemented.");
            }
        }

        private static byte[] DecompressBC1(byte[] data, int width, int height)
        {
            byte[] rgba = new byte[width * height * 4];
            int blocksX = width / 4;
            int blocksY = height / 4;
            int offset = 0;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    if (offset + 8 > data.Length) break;

                    ushort color0 = BitConverter.ToUInt16(data, offset);
                    ushort color1 = BitConverter.ToUInt16(data, offset + 2);
                    uint indices = BitConverter.ToUInt32(data, offset + 4);
                    offset += 8;

                    // Conversion from RGB565 to RGB888
                    int r0 = ((color0 >> 11) & 0x1F) * 255 / 31;
                    int g0 = ((color0 >> 5) & 0x3F) * 255 / 63;
                    int b0 = (color0 & 0x1F) * 255 / 31;

                    int r1 = ((color1 >> 11) & 0x1F) * 255 / 31;
                    int g1 = ((color1 >> 5) & 0x3F) * 255 / 63;
                    int b1 = (color1 & 0x1F) * 255 / 31;

                    for (int i = 0; i < 16; i++)
                    {
                        int px = (bx * 4) + (i % 4);
                        int py = (by * 4) + (i / 4);

                        if (px >= width || py >= height) continue;

                        int idx = (int)((indices >> (i * 2)) & 3);
                        int r = 0, g = 0, b = 0, a = 255;

                        if (color0 > color1)
                        {
                            switch (idx)
                            {
                                case 0: r = r0; g = g0; b = b0; break;
                                case 1: r = r1; g = g1; b = b1; break;
                                case 2: r = (2 * r0 + r1) / 3; g = (2 * g0 + g1) / 3; b = (2 * b0 + b1) / 3; break;
                                case 3: r = (r0 + 2 * r1) / 3; g = (g0 + 2 * g1) / 3; b = (b0 + 2 * b1) / 3; break;
                            }
                        }
                        else
                        {
                            switch (idx)
                            {
                                case 0: r = r0; g = g0; b = b0; break;
                                case 1: r = r1; g = g1; b = b1; break;
                                case 2: r = (r0 + r1) / 2; g = (g0 + g1) / 2; b = (b0 + b1) / 2; break;
                                case 3: r = 0; g = 0; b = 0; a = 255; break;
                            }
                        }

                        int pIdx = (py * width + px) * 4;
                        rgba[pIdx] = (byte)r;
                        rgba[pIdx + 1] = (byte)g;
                        rgba[pIdx + 2] = (byte)b;
                        rgba[pIdx + 3] = (byte)a;
                    }
                }
            }
            return rgba;
        }

        private static byte[] DecompressBC5(byte[] data, int width, int height)
        {
            byte[] rgba = new byte[width * height * 4];
            int blocksX = width / 4;
            int blocksY = height / 4;
            int offset = 0;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    if (offset + 16 > data.Length) break;

                    // Decodes the Red channel (first 8 bytes)
                    DecodeBC4Block(data, offset, rgba, width, height, bx, by, 0);

                    // Decode the Green channel (next 8 bytes)
                    DecodeBC4Block(data, offset + 8, rgba, width, height, bx, by, 1);

                    for (int i = 0; i < 16; i++)
                    {
                        int px = (bx * 4) + (i % 4);
                        int py = (by * 4) + (i / 4);

                        if (px >= width || py >= height) continue;

                        int pIdx = (py * width + px) * 4;

                        // Set Blue and Alpha to 255
                        rgba[pIdx + 2] = 255;
                        rgba[pIdx + 3] = 255;
                    }
                    offset += 16;
                }
            }
            return rgba;
        }

        private static void DecodeBC4Block(byte[] data, int offset, byte[] rgba, int width, int height, int bx, int by, int channelOffset)
        {
            byte r0 = data[offset];
            byte r1 = data[offset + 1];

            // Reads the 6 index bytes (48 bits) cleanly
            ulong indices = 0;
            for (int b = 0; b < 6; b++)
            {
                indices |= ((ulong)data[offset + 2 + b]) << (b * 8);
            }

            byte[] palette = new byte[8];
            palette[0] = r0;
            palette[1] = r1;

            if (r0 > r1)
            {
                palette[2] = (byte)((6 * r0 + 1 * r1) / 7);
                palette[3] = (byte)((5 * r0 + 2 * r1) / 7);
                palette[4] = (byte)((4 * r0 + 3 * r1) / 7);
                palette[5] = (byte)((3 * r0 + 4 * r1) / 7);
                palette[6] = (byte)((2 * r0 + 5 * r1) / 7);
                palette[7] = (byte)((1 * r0 + 6 * r1) / 7);
            }
            else
            {
                palette[2] = (byte)((4 * r0 + 1 * r1) / 5);
                palette[3] = (byte)((3 * r0 + 2 * r1) / 5);
                palette[4] = (byte)((2 * r0 + 3 * r1) / 5);
                palette[5] = (byte)((1 * r0 + 4 * r1) / 5);
                palette[6] = 0;
                palette[7] = 255;
            }

            for (int i = 0; i < 16; i++)
            {
                int px = (bx * 4) + (i % 4);
                int py = (by * 4) + (i / 4);
                if (px >= width || py >= height) continue;

                int idx = (int)((indices >> (i * 3)) & 7);
                int pIdx = (py * width + px) * 4;
                rgba[pIdx + channelOffset] = palette[idx];
            }
        }

        /// <summary>
        /// Compresses linear RGBA8 data into raw BC format.
        /// </summary>
        public static byte[] CompressBC(byte[] rgbaData, int width, int height, int version)
        {
            string tmpPng = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            string tmpPvr = Path.ChangeExtension(Path.GetTempFileName(), ".pvr");

            try
            {
                // Ensure version is supported
                if (version != 1 && version != 3 && version != 4 && version != 5 && version != 7)
                    throw new ArgumentException("Version must be 1, 3, 4, 5, or 7", nameof(version));

                WriteRgbaAsPng(rgbaData, width, height, tmpPng);

                // Build argument for etcpak (e.g., "bc1", "bc3")
                string formatArg = $"bc{version}";

                // Run etcpak using standard compression parameters
                RunEtcpak($"-c {formatArg} \"{tmpPng}\" \"{tmpPvr}\"");

                return ReadPvrPayload(tmpPvr);
            }
            finally
            {
                TryDelete(tmpPng);
                TryDelete(tmpPvr);
            }
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }
}