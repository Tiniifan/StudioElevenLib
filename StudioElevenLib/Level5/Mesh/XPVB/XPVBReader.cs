using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Mesh.XPVB
{
    public class XPVBReader
    {
        private readonly Stream _baseStream;
        private readonly List<uint> _nodeTable;

        public XPVBReader(Stream stream, List<uint> nodeTable)
        {
            _baseStream = stream;
            _nodeTable = nodeTable;
        }

        public List<XPVBSupport.Vertex> Read()
        {
            var vertices = new List<XPVBSupport.Vertex>();

            using (var reader = new BinaryDataReader(_baseStream))
            {
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));

                if (magic != "XPVB")
                {
                    throw new InvalidDataException(
                        $"Invalid XPVB file format. Expected magic 'XPVB' but found '{magic}'."
                    );
                }

                ushort attBufferOffset = reader.ReadValue<ushort>();
                ushort unkOffset = reader.ReadValue<ushort>();
                ushort vertexBufferOffset = reader.ReadValue<ushort>();
                ushort stride = reader.ReadValue<ushort>();
                uint vertexCount = reader.ReadValue<uint>();

                // Read AttBuffer
                reader.Seek(attBufferOffset);
                byte[] attCompressed = reader.ReadMultipleValue<byte>(unkOffset - attBufferOffset);
                byte[] attDecoded = Compressor.Decompress(attCompressed);

                int[] aCount = new int[10];
                int[] aOffset = new int[10];
                int[] aSize = new int[10];
                int[] aType = new int[10];

                using (var attStream = new MemoryStream(attDecoded))
                using (var attReader = new BinaryDataReader(attStream))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        aCount[i] = attReader.ReadValue<byte>();
                        aOffset[i] = attReader.ReadValue<byte>();
                        aSize[i] = attReader.ReadValue<byte>();
                        aType[i] = attReader.ReadValue<byte>();
                    }
                }

                // Read Vertex Buffer
                reader.Seek(vertexBufferOffset);
                byte[] vtxCompressed = reader.ReadMultipleValue<byte>((int)(reader.Length - vertexBufferOffset));
                byte[] vtxDecoded = Compressor.Decompress(vtxCompressed);

                using (var vtxStream = new MemoryStream(vtxDecoded))
                using (var vtxReader = new BinaryDataReader(vtxStream))
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        var vertex = new XPVBSupport.Vertex();

                        // Pos (j=0)
                        if (aCount[0] > 0)
                        {
                            vtxReader.Seek(i * stride + aOffset[0]);
                            vertex.Position.X = vtxReader.ReadValue<float>();
                            vertex.Position.Y = vtxReader.ReadValue<float>();
                            vertex.Position.Z = vtxReader.ReadValue<float>();
                        }

                        // Normal (j=2)
                        if (aCount[2] > 0)
                        {
                            vtxReader.Seek(i * stride + aOffset[2]);
                            vertex.Normal.X = vtxReader.ReadValue<float>();
                            vertex.Normal.Y = vtxReader.ReadValue<float>();
                            vertex.Normal.Z = vtxReader.ReadValue<float>();
                        }

                        // UV0 (j=4) - Reverse Y axis
                        if (aCount[4] > 0)
                        {
                            vtxReader.Seek(i * stride + aOffset[4]);
                            vertex.UV0.X = vtxReader.ReadValue<float>();
                            vertex.UV0.Y = 1.0f - vtxReader.ReadValue<float>();
                        }

                        // UV1 (j=5)
                        if (aCount[5] > 0)
                        {
                            vtxReader.Seek(i * stride + aOffset[5]);
                            vertex.UV1.X = vtxReader.ReadValue<float>();
                            vertex.UV1.Y = 1.0f - vtxReader.ReadValue<float>();
                        }

                        // Weights (j=7)
                        if (aCount[7] > 0)
                        {
                            vtxReader.Seek(i * stride + aOffset[7]);
                            vertex.Weights.X = vtxReader.ReadValue<float>();
                            vertex.Weights.Y = aCount[7] > 1 ? vtxReader.ReadValue<float>() : 0;
                            vertex.Weights.Z = aCount[7] > 2 ? vtxReader.ReadValue<float>() : 0;
                            vertex.Weights.W = aCount[7] > 3 ? vtxReader.ReadValue<float>() : 0;
                        }

                        // Bone Indices (j=8)
                        if (aCount[8] > 0)
                        {
                            vtxReader.Seek(i * stride + aOffset[8]);
                            float b0 = vtxReader.ReadValue<float>();
                            float b1 = aCount[8] > 1 ? vtxReader.ReadValue<float>() : 0;
                            float b2 = aCount[8] > 2 ? vtxReader.ReadValue<float>() : 0;
                            float b3 = aCount[8] > 3 ? vtxReader.ReadValue<float>() : 0;

                            if (_nodeTable != null && _nodeTable.Count > 0)
                            {
                                vertex.BoneIndices = new System.Numerics.Vector4(
                                    _nodeTable[(int)b0], _nodeTable[(int)b1], _nodeTable[(int)b2], _nodeTable[(int)b3]);
                            }
                            else
                            {
                                vertex.BoneIndices = new System.Numerics.Vector4(b0, b1, b2, b3);
                            }
                        }

                        // Color (j=9)
                        if (aCount[9] > 0)
                        {
                            vtxReader.Seek(i * stride + aOffset[9]);
                            vertex.Color.X = vtxReader.ReadValue<float>();
                            vertex.Color.Y = aCount[9] > 1 ? vtxReader.ReadValue<float>() : 0;
                            vertex.Color.Z = aCount[9] > 2 ? vtxReader.ReadValue<float>() : 0;
                            vertex.Color.W = aCount[9] > 3 ? vtxReader.ReadValue<float>() : 0;
                        }

                        vertices.Add(vertex);
                    }
                }
            }
            return vertices;
        }
    }
}
