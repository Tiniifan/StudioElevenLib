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

                // Read the block holding the value of every fixed attribute
                byte[] fixedDecoded = new byte[0];
                if (vertexBufferOffset > unkOffset)
                {
                    reader.Seek(unkOffset);
                    byte[] fixedCompressed = reader.ReadMultipleValue<byte>(vertexBufferOffset - unkOffset);
                    fixedDecoded = Compressor.Decompress(fixedCompressed);
                }

                // Read Vertex Buffer
                reader.Seek(vertexBufferOffset);
                byte[] vtxCompressed = reader.ReadMultipleValue<byte>((int)(reader.Length - vertexBufferOffset));
                byte[] vtxDecoded = Compressor.Decompress(vtxCompressed);

                using (var vtxStream = new MemoryStream(vtxDecoded))
                using (var vtxReader = new BinaryDataReader(vtxStream))
                {
                    float[] ReadAttribute(int index, int slot)
                    {
                        var values = new float[4];

                        if (aCount[slot] == 0)
                        {
                            return values;
                        }

                        if (aType[slot] == 1)
                        {
                            // A fixed attribute is one value for the whole mesh, always 4 floats at a 4 bytes aligned offset
                            int fixedOffset = aOffset[slot] & 0xFC;

                            if (fixedDecoded.Length >= fixedOffset + 16)
                            {
                                for (int c = 0; c < 4; c++)
                                {
                                    values[c] = BitConverter.ToSingle(fixedDecoded, fixedOffset + c * 4);
                                }
                            }
                        }
                        else if (aType[slot] == 2)
                        {
                            vtxReader.Seek(index * stride + aOffset[slot]);

                            for (int c = 0; c < aCount[slot] && c < 4; c++)
                            {
                                values[c] = vtxReader.ReadValue<float>();
                            }
                        }

                        return values;
                    }

                    for (int i = 0; i < vertexCount; i++)
                    {
                        var vertex = new XPVBSupport.Vertex();

                        // Pos (j=0)
                        if (aCount[0] > 0)
                        {
                            float[] position = ReadAttribute(i, 0);
                            vertex.Position = new System.Numerics.Vector3(position[0], position[1], position[2]);
                        }

                        // Tint (j=1)
                        if (aCount[1] > 0)
                        {
                            float[] tint = ReadAttribute(i, 1);
                            vertex.Tint = new System.Numerics.Vector4(tint[0], tint[1], tint[2], tint[3]);
                        }

                        // Normal (j=2)
                        if (aCount[2] > 0)
                        {
                            float[] normal = ReadAttribute(i, 2);
                            vertex.Normal = new System.Numerics.Vector3(normal[0], normal[1], normal[2]);
                        }

                        // UV0 (j=4) - Reverse Y axis
                        if (aCount[4] > 0)
                        {
                            float[] uv0 = ReadAttribute(i, 4);
                            vertex.UV0 = new System.Numerics.Vector2(uv0[0], 1.0f - uv0[1]);
                        }

                        // UV1 (j=5)
                        if (aCount[5] > 0)
                        {
                            float[] uv1 = ReadAttribute(i, 5);
                            vertex.UV1 = new System.Numerics.Vector2(uv1[0], 1.0f - uv1[1]);
                        }

                        // Weights (j=7)
                        if (aCount[7] > 0)
                        {
                            float[] weights = ReadAttribute(i, 7);
                            vertex.Weights = new System.Numerics.Vector4(weights[0], weights[1], weights[2], weights[3]);
                        }

                        // Bone Indices (j=8)
                        if (aCount[8] > 0)
                        {
                            float[] boneIndices = ReadAttribute(i, 8);

                            if (_nodeTable != null && _nodeTable.Count > 0)
                            {
                                vertex.BoneIndices = new System.Numerics.Vector4(
                                    _nodeTable[(int)boneIndices[0]], _nodeTable[(int)boneIndices[1]],
                                    _nodeTable[(int)boneIndices[2]], _nodeTable[(int)boneIndices[3]]);
                            }
                            else
                            {
                                vertex.BoneIndices = new System.Numerics.Vector4(
                                    boneIndices[0], boneIndices[1], boneIndices[2], boneIndices[3]);
                            }
                        }

                        // Color (j=9)
                        if (aCount[9] > 0)
                        {
                            float[] color = ReadAttribute(i, 9);
                            vertex.Color = new System.Numerics.Vector4(color[0], color[1], color[2], color[3]);
                        }

                        vertices.Add(vertex);
                    }
                }
            }
            return vertices;
        }
    }
}
