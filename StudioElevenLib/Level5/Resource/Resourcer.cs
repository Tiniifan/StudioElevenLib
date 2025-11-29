using System;
using System.IO;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;

namespace StudioElevenLib.Level5.Resource
{
    /// <summary>
    /// Provides methods to create and read different types of L5 resources,
    /// </summary>
    public class Resourcer
    {
        /// <summary>
        /// Returns an IResource instance from a byte array.
        /// </summary>
        public static IResource GetResource(byte[] data)
        {
            return GetResource(new MemoryStream(data));
        }

        /// <summary>
        /// Returns an IResource instance from a stream by detecting the resource type.
        /// </summary>
        public static IResource GetResource(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("The provided stream doesn't support reading.");
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using (BinaryDataReader reader = new BinaryDataReader(Compressor.Decompress(memoryStream)))
                {
                    long magicLong = reader.ReadValue<long>();
                    string magicSTR = ResourceHelper.LongToUtf8String(magicLong);

                    reader.Seek(0);

                    IResource resource = null;

                    if (magicSTR.StartsWith("CHR"))
                    {
                        // RES Scene3D
                        resource = new RES.RES(reader);
                    }
                    else if (magicSTR.StartsWith("XRES"))
                    {
                        // XRES Scene3D
                        resource = new XRES.XRES(reader);
                    }
                    else if (magicSTR.StartsWith("ANMC00"))
                    {
                        // RES Scene2D
                        throw new NotSupportedException("RES Scene2D (ANMC00) is not supported yet.");
                    }
                    else if (magicSTR.StartsWith("XA01"))
                    {
                        // XRES Scene2D
                        throw new NotSupportedException("XRES Scene2D (XA01) is not supported yet.");
                    }
                    else
                    {
                        throw new InvalidDataException("Invalid or unknown resource magic: '" + magicSTR + "'.");
                    }

                    return resource;
                }
            }
        }
    }
}