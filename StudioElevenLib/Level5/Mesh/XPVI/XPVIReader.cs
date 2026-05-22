using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;

namespace StudioElevenLib.Level5.Mesh.XPVI
{
    public class XPVIReader
    {
        private readonly Stream _baseStream;

        public XPVIReader(Stream stream)
        {
            _baseStream = stream;
        }

        public List<ushort> Read()
        {
            var triangles = new List<ushort>();

            using (var reader = new BinaryDataReader(_baseStream))
            {
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));

                if (magic != "XPVI")
                {
                    throw new InvalidDataException(
                        $"Invalid XPVI file format. Expected magic 'XPVI' but found '{magic}'."
                    );
                }

                ushort primitiveType = reader.ReadValue<ushort>();
                ushort facesOffset = reader.ReadValue<ushort>();
                uint faceCount = reader.ReadValue<uint>();

                reader.Seek(facesOffset);
                byte[] compressedFaces = reader.ReadMultipleValue<byte>((int)(reader.Length - facesOffset));
                byte[] decodedFaces = Compressor.Decompress(compressedFaces);

                using (var ibuffer = new MemoryStream(decodedFaces))
                using (var indexReader = new BinaryDataReader(ibuffer))
                {
                    if (primitiveType == 0)
                    {
                        // Indice list
                        for (int i = 0; i < faceCount; i++)
                        {
                            triangles.Add(indexReader.ReadValue<ushort>());
                        }
                    }
                    else if (primitiveType == 2)
                    {
                        // Triangle strip
                        ushort[] strip = new ushort[faceCount];
                        for (int i = 0; i < faceCount; i++)
                        {
                            strip[i] = indexReader.ReadValue<ushort>();
                        }

                        bool flip = false;
                        for (int i = 0; i < faceCount - 2; i++)
                        {
                            ushort v0 = strip[i];
                            ushort v1 = strip[i + 1];
                            ushort v2 = strip[i + 2];

                            // We ignore degenerate triangles
                            if (v0 != v1 && v1 != v2 && v2 != v0)
                            {
                                if (flip)
                                {
                                    // Reverse order to keep the front facing forward
                                    triangles.Add(v1);
                                    triangles.Add(v0);
                                    triangles.Add(v2);
                                }
                                else
                                {
                                    // Normal order
                                    triangles.Add(v0);
                                    triangles.Add(v1);
                                    triangles.Add(v2);
                                }
                            }

                            // We reverse the order at each iteration
                            flip = !flip;
                        }
                    }
                }
            }

            return triangles;
        }
    }
}