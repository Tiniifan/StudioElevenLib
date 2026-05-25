#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using StudioElevenLib.Level5.Image.IMGC;

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
    /// Groups all IMGC image commands: decode, decode-raw, encode.
    /// Register this module in Program.cs to make all three commands available.
    /// </summary>
    public class ImageModule : IModule
    {
        public string Name => "image";
        public string Description => "IMGC (.xi) image encode / decode operations";

        public IReadOnlyList<ICommand> Commands { get; } = new List<ICommand>
        {
            new DecodeCommand(),
            new DecodeRawCommand(),
            new EncodeCommand(),
        };
    }

    /// <summary>
    /// Reads a Base64-encoded IMGC file from stdin and writes a
    /// Base64-encoded PNG to stdout.
    /// </summary>
    internal sealed class DecodeCommand : ICommand
    {
        public string Name => "decode";
        public string Description => "Decode an IMGC (.xi) file to a PNG image";
        public string Help =>
            "Usage: exe decode\n" +
            "\n" +
            "  Reads  : Base64-encoded IMGC (.xi) data from stdin\n" +
            "  Writes : Base64-encoded PNG to stdout\n" +
            "\n" +
            "  Example (Python):\n" +
            "    proc = subprocess.run(['exe', 'decode'], input=b64_xi, capture_output=True)\n" +
            "    png_b64 = proc.stdout";

        public void Execute(string[] args)
        {
            byte[] inputBytes = CommandExtensions.ReadStdinBase64(this);

            using var msIn = new MemoryStream(inputBytes);
            var imgc = new IMGC(msIn);

            using var msOut = new MemoryStream();

#if USE_SYSTEM_DRAWING
            imgc.Bitmap!.Save(msOut, ImageFormat.Png);
#elif USE_IMAGESHARP
            imgc.Bitmap!.SaveAsPng(msOut);
#endif

            Console.Write(Convert.ToBase64String(msOut.ToArray()));
        }
    }

    /// <summary>
    /// Reads a Base64-encoded IMGC file from stdin and writes a JSON object
    /// containing the image dimensions and raw RGBA pixel data to stdout.
    ///
    /// JSON schema:
    /// {
    ///   "width":  &lt;int&gt;,
    ///   "height": &lt;int&gt;,
    ///   "pixels": "&lt;Base64 of raw RGBA bytes, 4 bytes per pixel, row-major&gt;"
    /// }
    /// </summary>
    internal sealed class DecodeRawCommand : ICommand
    {
        public string Name => "decode-raw";
        public string Description => "Decode an IMGC file and return width, height and raw RGBA pixels as JSON";
        public string Help =>
            "Usage: exe decode-raw\n" +
            "\n" +
            "  Reads  : Base64-encoded IMGC (.xi) data from stdin\n" +
            "  Writes : JSON to stdout:\n" +
            "    {\n" +
            "      \"width\":  <int>,\n" +
            "      \"height\": <int>,\n" +
            "      \"pixels\": \"<Base64>\"   // raw RGBA bytes, 4 bytes/pixel, row-major\n" +
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

            using var msIn = new MemoryStream(inputBytes);
            var imgc = new IMGC(msIn);

            int width = imgc.Width;
            int height = imgc.Height;
            Color[] pixels = imgc.Pixels ?? throw new Exception("Pixel data was not decoded.");

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
                pixels = Convert.ToBase64String(rawPixels),
            });

            Console.Write(json);
        }
    }

    /// <summary>
    /// Reads a Base64-encoded PNG from stdin and writes a Base64-encoded
    /// IMGC (.xi) file to stdout, using the given pixel format.
    /// </summary>
    internal sealed class EncodeCommand : ICommand
    {
        public string Name => "encode";
        public string Description => "Encode a PNG image to IMGC (.xi) format";
        public string Help =>
            "Usage: exe encode <format_hex> <is_switch>\n" +
            "\n" +
            "  format_hex   Pixel format as a hex byte, e.g. 0x1B\n" +
            "  is_switch    true  → Nintendo Switch format table\n" +
            "               false → 3DS format table\n" +
            "\n" +
            "  Reads  : Base64-encoded PNG from stdin\n" +
            "  Writes : Base64-encoded IMGC (.xi) to stdout\n" +
            "\n" +
            "  Example:\n" +
            "    exe encode 0x1B true";

        public void Execute(string[] args)
        {
            if (args.Length < 3)
                throw new Exception(
                    "Missing arguments for 'encode'. " +
                    "Run: exe encode --help");

            // Accept both "0x1B" and "1B"
            string formatStr = args[1].Replace("0x", "").Replace("0X", "");
            byte formatByte = Convert.ToByte(formatStr, 16);
            bool isSwitch = bool.Parse(args[2]);

            var formatDict = isSwitch
                ? IMGCSupport.SwitchPixelFormats
                : IMGCSupport.PixelFormats3DS;

            if (!formatDict.TryGetValue(formatByte, out var pixelFormat))
                throw new Exception(
                    $"Format '{args[1]}' is not supported for " +
                    $"{(isSwitch ? "Switch" : "3DS")}.");

            byte[] inputBytes = CommandExtensions.ReadStdinBase64(this);

            using var msIn = new MemoryStream(inputBytes);

#if USE_SYSTEM_DRAWING
            var bitmap = new Bitmap(msIn);
#elif USE_IMAGESHARP
            var bitmap = Image.Load<Rgba32>(msIn);
#endif

            var imgc = new IMGC(bitmap, pixelFormat);
            byte[] xiBytes = imgc.Save(isSwitch);

            Console.Write(Convert.ToBase64String(xiBytes));
        }
    }

}
