#nullable enable

using System;
using System.IO;

using StudioElevenLib.Common.Env;
using StudioElevenLib.Common.Imaging;
using StudioElevenLib.Common.Env.Platforms;
using StudioElevenLib.Level5.Image.Profiles;
using System.Text;


#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image
{
    public static class Imager
    {
        /// <summary>
        /// Reads the magic header, detects the platform and version, and routes to the correct profile.
        /// </summary>
        public static Image5 Open(Stream stream)
        {
            if (stream == null || stream.Length < 6)
                throw new ArgumentException("Stream is invalid or too small.");

            byte[] headerBytes = new byte[6];
            int bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);
            if (bytesRead != headerBytes.Length)
            {
                throw new EndOfStreamException("Unable to read the image header.");
            }

            stream.Position -= headerBytes.Length; // Rewind for the actual reader

            string magic = Encoding.ASCII.GetString(headerBytes, 0, 4);
            string versionStr = Encoding.ASCII.GetString(headerBytes, 4, 2);

            if (!int.TryParse(versionStr, out int version))
            {
                throw new InvalidDataException($"Invalid version format in header: {versionStr}");
            }

            Image5Profile profile = magic switch
            {
                "IMGC" => new IMGCProfile(),
                "IMGN" => new IMGNProfile(),
                "IMGA" => throw new NotSupportedException("Android format (IMGA) is not supported yet."),
                _ => throw new InvalidDataException($"Unknown image header magic: {magic}")
            };

            return new Image5(stream, profile, version);
        }

        public static Image5 Open(byte[] data)
        {
            using var ms = new MemoryStream(data);
            return Open(ms);
        }

        /// <summary>
        /// Creates a new Image5 from a bitmap and a target platform name.
        /// </summary>
        public static Image5 Create(
#if USE_SYSTEM_DRAWING
            Bitmap bitmap,
#elif USE_IMAGESHARP
            SixLabors.ImageSharp.Image<Rgba32> bitmap,
#endif
            IPixelFormat format,
            string platformName,
            int version = 0)
        {
            var profile = ProfileFromPlatformName(platformName);
            return new Image5(bitmap, format, profile, version);
        }

        /// <summary>
        /// Creates a new Image5 from a bitmap and a platform instance.
        /// </summary>
        public static Image5 Create(
#if USE_SYSTEM_DRAWING
            Bitmap bitmap,
#elif USE_IMAGESHARP
            SixLabors.ImageSharp.Image<Rgba32> bitmap,
#endif
            IPixelFormat format,
            IPlatform platform,
            int version = 0)
        {
            var profile = ProfileFromPlatform(platform);
            return new Image5(bitmap, format, profile, version);
        }

        public static Image5Profile ProfileFromPlatform(IPlatform platform)
        {
            return platform switch
            {
                CTRPlatform => new IMGCProfile(),
                NXPlatform => new IMGNProfile(),
                _ => throw new NotSupportedException(
                    $"No Image5 profile for platform '{platform.Name}'.")
            };
        }

        private static Image5Profile ProfileFromPlatformName(string name)
        {
            return ProfileFromPlatform(Platforms.FromName(name));
        }
    }
}