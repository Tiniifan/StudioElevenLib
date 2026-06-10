#nullable enable

using System;
using System.IO;
using System.Collections.Generic;

using System.Text.Json;
using StudioElevenLib.Level5.Image;
using StudioElevenLib.Common.Env.Platforms;

#if USE_SYSTEM_DRAWING
using System.Drawing;
using System.Drawing.Imaging;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioEleven.Modules
{
    /// <summary>
    /// Groups all Image5 commands: decode, decode-raw, encode.
    /// Register this module in Program.cs to make all three commands available.
    /// </summary>
    public class ImageModule : IModule
    {
        public string Name => "image";
        public string Description => "Image5 (.xi) image encode / decode operations";

        public IReadOnlyList<ICommand> Commands { get; } = new List<ICommand>
        {
            new DecodeCommand(),
            new DecodeRawCommand(),
            new EncodeCommand(),
        };
    }

    /// <summary>
    /// Reads a Base64-encoded Image5 file from stdin and writes a
    /// Base64-encoded PNG to stdout.
    /// </summary>
    internal sealed class DecodeCommand : ICommand
    {
        public string Name => "decode";
        public string Description => "Decode an Image5 (.xi) file to a PNG image";
        public string Help =>
            "Usage: exe image decode\n" +
            "\n" +
            "  Reads  : Base64-encoded Image5 (.xi) data from stdin\n" +
            "  Writes : Base64-encoded PNG to stdout\n" +
            "\n" +
            "  Example (Python):\n" +
            "    proc = subprocess.run(['exe', 'image', 'decode'], input=b64_xi, capture_output=True)\n" +
            "    png_b64 = proc.stdout";

        public void Execute(string[] args)
        {
            byte[] inputBytes = CommandExtensions.ReadStdinBase64(this);

            var image = Imager.Open(inputBytes);

            using var msOut = new MemoryStream();

#if USE_SYSTEM_DRAWING
            image.Bitmap!.Save(msOut, ImageFormat.Png);
#elif USE_IMAGESHARP
            image.Bitmap!.SaveAsPng(msOut);
#endif

            Console.Write(Convert.ToBase64String(msOut.ToArray()));
        }
    }

    /// <summary>
    /// Reads a Base64-encoded Image5 file from stdin and writes a JSON object
    /// containing the image dimensions, header info, version, format, and raw RGBA pixel data to stdout.
    ///
    /// JSON schema:
    /// {
    ///   "width":        &lt;int&gt;,
    ///   "height":       &lt;int&gt;,
    ///   "profile":      "&lt;string&gt;",   // e.g. "IMGC" or "IMGN"
    ///   "version":      &lt;int&gt;,
    ///   "format_name":  "&lt;string&gt;",   // e.g. "ETC1"
    ///   "format_byte":  &lt;int&gt;,        // e.g. 27  (0x1B)
    ///   "pixels":       "&lt;Base64 of raw RGBA bytes, 4 bytes per pixel, row-major&gt;"
    /// }
    /// </summary>
    internal sealed class DecodeRawCommand : ICommand
    {
        public string Name => "decode-raw";
        public string Description => "Decode an Image5 file and return width, height, profile, version, format and raw RGBA pixels as JSON";
        public string Help =>
            "Usage: exe image decode-raw\n" +
            "\n" +
            "  Reads  : Base64-encoded Image5 (.xi) data from stdin\n" +
            "  Writes : JSON to stdout:\n" +
            "    {\n" +
            "      \"width\":       <int>,\n" +
            "      \"height\":      <int>,\n" +
            "      \"profile\":     \"<string>\",   // e.g. \"IMGC\" or \"IMGN\"\n" +
            "      \"version\":     <int>,\n" +
            "      \"format_name\": \"<string>\",   // e.g. \"ETC1\"\n" +
            "      \"format_byte\": <int>,         // e.g. 27 (0x1B)\n" +
            "      \"pixels\":      \"<Base64>\"    // raw RGBA bytes, 4 bytes/pixel, row-major\n" +
            "    }\n" +
            "\n" +
            "  Example (Python):\n" +
            "    import json, base64, numpy as np\n" +
            "    result = json.loads(proc.stdout)\n" +
            "    arr = np.frombuffer(base64.b64decode(result['pixels']), dtype=np.uint8)\n" +
            "    arr = arr.reshape((result['height'], result['width'], 4))";

        public void Execute(string[] args)
        {
            byte[] inputBytes = CommandExtensions.ReadStdinBase64(this);
            var image = Imager.Open(inputBytes);

            int width = image.Width;
            int height = image.Height;
            string profile = image.Profile.Name;
            int version = image.Version;

            // Resolve the format byte key from the profile's dictionary
            var imageFormat = image.ImageFormat
                ?? throw new Exception("Image format was not decoded.");

            var formatPair = image.Profile.PixelFormats
                .FirstOrDefault(kv => kv.Value?.Name == imageFormat.Name);

            if (formatPair.Value == null)
                throw new Exception($"Format '{imageFormat.Name}' not found in profile '{profile}' dictionary.");

            string formatName = imageFormat.Name;
            int formatByte = formatPair.Key;

            Color[] pixels = image.Pixels
                ?? throw new Exception("Pixel data was not decoded.");

            // Pack every pixel into a flat RGBA byte array (4 bytes per pixel, row-major)
            byte[] rawPixels = new byte[width * height * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];

#if USE_SYSTEM_DRAWING
            // System.Drawing.Color exposes R/G/B/A directly
            rawPixels[i * 4 + 0] = c.R;
            rawPixels[i * 4 + 1] = c.G;
            rawPixels[i * 4 + 2] = c.B;
            rawPixels[i * 4 + 3] = c.A;
#elif USE_IMAGESHARP
                // ImageSharp Color needs to be unpacked via ToPixel<Rgba32>()
                var rgba = c.ToPixel<Rgba32>();
                rawPixels[i * 4 + 0] = rgba.R;
                rawPixels[i * 4 + 1] = rgba.G;
                rawPixels[i * 4 + 2] = rgba.B;
                rawPixels[i * 4 + 3] = rgba.A;
#endif
            }

            // Serialize to JSON — kept compact for easy piping
            string json = JsonSerializer.Serialize(new
            {
                width,
                height,
                profile,
                version,
                format_name = formatName,
                format_byte = formatByte,
                pixels = Convert.ToBase64String(rawPixels),
            });

            Console.Write(json);
        }
    }

    /// <summary>
    /// Reads a Base64-encoded PNG from stdin and writes a Base64-encoded
    /// Image5 (.xi) file to stdout, using the given pixel format and target platform.
    /// </summary>
    internal sealed class EncodeCommand : ICommand
    {
        public string Name => "encode";
        public string Description => "Encode a PNG image to Image5 (.xi) format";
        public string Help =>
            "Usage: exe image encode <format_hex> <platform> [version]\n" +
            "\n" +
            "  format_hex   Pixel format as a hex byte, e.g. 0x1B\n" +
            "  platform     Target platform name: CTR, NX, ANDROID\n" +
            "               CTR  → 3DS   (IMGCProfile)\n" +
            "               NX   → Switch (IMGNProfile)\n" +
            "  version      (optional) Header version integer, default 0\n" +
            "\n" +
            "  Reads  : Base64-encoded PNG from stdin\n" +
            "  Writes : Base64-encoded Image5 (.xi) to stdout\n" +
            "\n" +
            "  Example:\n" +
            "    exe image encode 0x1B CTR\n" +
            "    exe image encode 0x1B NX 1";

        public void Execute(string[] args)
        {
            if (args.Length < 3)
                throw new Exception(
                    "Missing arguments for 'encode'. " +
                    "Run: exe image encode --help");

            // Accept both "0x1B" and "1B"
            string formatStr = args[1].Replace("0x", "").Replace("0X", "");
            byte formatByte = Convert.ToByte(formatStr, 16);
            string platformName = args[2];
            int version = args.Length >= 4 && int.TryParse(args[3], out int v) ? v : 0;

            // Resolve platform and derive the matching pixel format dictionary
            var platform = Platforms.FromName(platformName);
            var profile = Imager.ProfileFromPlatform(platform);

            if (!profile.PixelFormats.TryGetValue(formatByte, out var pixelFormat))
                throw new Exception(
                    $"Format '0x{formatByte:X2}' is not supported for platform '{platformName}'.");

            byte[] inputBytes = CommandExtensions.ReadStdinBase64(this);
            using var msIn = new MemoryStream(inputBytes);

#if USE_SYSTEM_DRAWING
            var bitmap = new Bitmap(msIn);
#elif USE_IMAGESHARP
            var bitmap = SixLabors.ImageSharp.Image.Load<Rgba32>(msIn);
#endif

            var image = Imager.Create(bitmap, pixelFormat, platform, version);
            byte[] xiBytes = image.Save();

            Console.Write(Convert.ToBase64String(xiBytes));
        }
    }
}