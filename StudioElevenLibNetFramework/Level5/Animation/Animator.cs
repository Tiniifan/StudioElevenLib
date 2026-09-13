using System;
using System.IO;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;

namespace StudioElevenLib.Level5.Animation
{
    /// <summary>
    /// Provides methods to create and read different versions of L5 animations,
    /// </summary>
    public static class Animator
    {
        /// <summary>
        /// Returns an IAnimationManager instance from a byte array.
        /// </summary>
        public static IAnimationManager GetAnimation(byte[] data)
        {
            switch (GetVersion(data))
            {
                case "V1":
                    return new AnimationManagerV1(new MemoryStream(data));
                case "V2":
                    return new AnimationManagerV2(new MemoryStream(data));
                case "V3":
                    return new AnimationManagerV3(new MemoryStream(data));
                default:
                    throw new InvalidDataException("Unknown animation version.");
            }
        }

        /// <summary>
        /// Returns an IAnimationManager instance from a stream by detecting the animation version.
        /// </summary>
        public static IAnimationManager GetAnimation(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("The provided stream doesn't support reading.");
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return GetAnimation(memoryStream.ToArray());
            }
        }

        /// <summary>
        /// Returns an empty IAnimationManager instance for the given version (V1, V2 or V3).
        /// </summary>
        public static IAnimationManager CreateAnimation(string version)
        {
            switch (version)
            {
                case "V1":
                    return new AnimationManagerV1();
                case "V2":
                    return new AnimationManagerV2();
                case "V3":
                    return new AnimationManagerV3();
                default:
                    throw new ArgumentException("Unknown animation version: '" + version + "'.");
            }
        }

        /// <summary>
        /// Detects the animation version (V1, V2 or V3) from a byte array.
        /// </summary>
        public static string GetVersion(byte[] data)
        {
            using (BinaryDataReader reader = new BinaryDataReader(data))
            {
                // V3 stores its header offsets right after the magic, V1 and V2 only have padding here
                reader.Seek(0x04);
                if (reader.ReadValue<int>() != 0)
                {
                    return "V3";
                }

                // Read header
                reader.Seek(0x0);
                AnimationSupport.Header header = AnimationSupport.ReadHeader(reader, out string format);

                // Get decomp block
                reader.Seek(header.CompDataOffset);
                using (BinaryDataReader decompReader = new BinaryDataReader(Compressor.Decompress(reader.GetSection((int)(reader.Length - reader.Position)))))
                {
                    int hashOffset = decompReader.ReadValue<int>();

                    if (hashOffset == 0x0C)
                    {
                        return "V2";
                    }
                    else
                    {
                        return "V1";
                    }
                }
            }
        }
    }
}
