using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression.LZ10;
using StudioElevenLib.Level5.Mesh.Logic;

namespace StudioElevenLib.Level5.Mesh
{
    public static class XMPR
    {
        public static List<string> UsedBones(Dictionary<int, Dictionary<int, float>> weights, List<string> boneNames)
        {
            var used = new Dictionary<int, string>();

            foreach (var key in weights.Keys)
            {
                foreach (var subKey in weights[key].Keys)
                {
                    if (!used.ContainsKey(subKey))
                    {
                        used[subKey] = boneNames[subKey];
                    }
                }
            }

            return new List<string>(used.Values);
        }

        public static Dictionary<int, Dictionary<int, float>> UsedWeights(Dictionary<int, Dictionary<int, float>> weights)
        {
            var used = new Dictionary<int, int>();
            int index = 0;

            var keys = new List<int>(weights.Keys);

            foreach (var key in keys)
            {
                var tempDict = weights[key];
                weights[key] = new Dictionary<int, float>();

                foreach (var subKey in new List<int>(tempDict.Keys))
                {
                    if (!used.ContainsKey(subKey))
                    {
                        used[subKey] = index;
                        index++;
                    }

                    weights[key][used[subKey]] = tempDict[subKey];
                }
            }

            return weights;
        }

        public static List<Face> RemoveDupeIndices(List<Face> faces)
        {
            var usedIndices = new List<Vector3>();
            var finalFaces = new List<Face>();

            foreach (var face in faces)
            {
                var geometrie = new Vector3(
                    face.Vertex,
                    face.Texture,
                    face.Normal
                );

                if (!usedIndices.Contains(geometrie))
                {
                    usedIndices.Add(geometrie);

                    finalFaces.Add(new Face(
                        face.Vertex,
                        face.Normal,
                        face.Texture,
                        face.Color
                    ));
                }
            }

            return finalFaces;
        }

        public static List<Vector3> IndexOfGeometrie(List<Face> faces)
        {
            var finalIndices = new List<Vector3>();
            var usedIndices = new List<Vector3>();

            foreach (var face in faces)
            {
                // Créer un vecteur pour chaque face à partir des indices de Vertex, Normal, Texture et Color
                var geometrie = new List<int> { face.Vertex, face.Normal, face.Texture, face.Color };

                var geometrieVector = new Vector3(
                    geometrie.Count > 0 ? geometrie[0] : 0,
                    geometrie.Count > 1 ? geometrie[1] : 0,
                    geometrie.Count > 2 ? geometrie[2] : 0
                );

                // Ajouter le vecteur si ce n'est pas déjà utilisé
                if (!usedIndices.Contains(geometrieVector))
                {
                    usedIndices.Add(geometrieVector);
                }
            }

            // Créer la liste finale d'indices basée sur les indices utilisés
            foreach (var face in faces)
            {
                var faceIndices = new List<int>
            {
                usedIndices.IndexOf(new Vector3(face.Vertex, 0, 0)),
                usedIndices.IndexOf(new Vector3(0, face.Normal, 0)),
                usedIndices.IndexOf(new Vector3(0, 0, face.Texture))
            };

                finalIndices.Add(new Vector3(faceIndices[0], faceIndices[1], faceIndices[2]));
            }

            return finalIndices;
        }

        public static byte[] WriteGeometrie(List<Face> faces, List<Vector3> vertices, List<Vector2> uvs, List<Vector3> normals, List<Vector4> colors, Dictionary<int, Dictionary<int, float>> weights)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryDataWriter writer = new BinaryDataWriter(memoryStream))
            {
                faces = RemoveDupeIndices(faces);

                foreach (var face in faces)
                {
                    // Write Vertice
                    if (face.Vertex != -1)
                    {
                        writer.Write(vertices[face.Vertex].X);
                        writer.Write(vertices[face.Vertex].Y);
                        writer.Write(vertices[face.Vertex].Z);

                        // Write Weight
                        writer.Seek(28);
                        var weight = weights[face.Vertex];
                        var keys = new List<int>(weight.Keys);
                        for (int w = 0; w < 4; w++)
                        {
                            if (w < keys.Count)
                            {
                                writer.Write(weight[keys[w]]);
                            }
                            else
                            {
                                writer.Write(0);
                            }
                        }

                        // Write Weight Index
                        for (int x = 0; x < 4; x++)
                        {
                            if (x < keys.Count)
                            {
                                writer.Write((float)keys[x]);
                            }
                            else
                            {
                                writer.Write(0);
                            }
                        }

                        writer.Seek(8);
                    }

                    // Normal
                    if (face.Normal != -1)
                    {
                        writer.Write(normals[face.Normal].X);
                        writer.Write(normals[face.Normal].Y);
                        writer.Write(normals[face.Normal].Z);
                    }

                    // Texture
                    if (face.Texture != -1)
                    {
                        writer.Write(uvs[face.Texture].X);
                        writer.Write(uvs[face.Texture].Y);
                    }

                    // Color
                    if (face.Color !=  -1)
                    {
                        writer.Write(colors[face.Color].X);
                        writer.Write(colors[face.Color].Y);
                        writer.Write(colors[face.Color].Z);
                        writer.Write(colors[face.Color].W);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        public static byte[] WriteTriangle(List<Face> faces)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryDataWriter writer = new BinaryDataWriter(memoryStream))
            {
                var indexedGeometries = IndexOfGeometrie(faces);
                var triangleStrip = Triangle.IndiceToStrip(indexedGeometries);

                foreach (int indice in triangleStrip)
                {
                    writer.Write((short)indice);
                }

                return memoryStream.ToArray();
            }
        }

        public static byte[] Write(string meshName, Vector3 dimensions, List<Face> indices, List<Vector3> vertices, List<Vector2> uvs, List<Vector3> normals, List<Vector4> colors, Dictionary<int, Dictionary<int, float>> weights, List<string> boneNames, string materialName, string[] mode)
        {
            // Récupérer les os utilisés
            boneNames = UsedBones(weights, boneNames);
            weights = UsedWeights(weights);

            // Obtenir les données de géométrie et de triangles
            var dataGeometrie = WriteGeometrie(indices, vertices, uvs, normals, colors, weights);
            var dataTriangle = WriteTriangle(indices);

            // XPVB-------------------------------------------
            byte[] xpvb = new byte[] { };
            using (BinaryDataWriter xpvbWriter = new BinaryDataWriter(xpvb))
            {
                var compressGeometrie = new LZ10().Compress(dataGeometrie);
                xpvbWriter.Write(new byte[] { 0x58, 0x50, 0x56, 0x42, 0x10, 0x00, 0x3C, 0x00, 0x48, 0x00, 0x58, 0x00 });
                xpvbWriter.Write(Convert.ToInt32(dataGeometrie.Length / 88));
                xpvbWriter.Write(new byte[]
                {
                        0x40, 0x01, 0x00, 0x00, 0x03, 0x00, 0x0C, 0x02, 0x04, 0x00, 0x10, 0x01, 0x03, 0x0C, 0x0C,
                        0x02, 0x00, 0x00, 0x00, 0x00, 0x02, 0x18, 0x08, 0x02, 0x02, 0x20, 0x08, 0x02, 0x00, 0x00,
                        0x00, 0x00, 0x04, 0x28, 0x10, 0x02, 0x04, 0x38, 0x10, 0x02, 0x04, 0x48, 0x10, 0x02, 0x81,
                        0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x80, 0x3F, 0x90, 0x03, 0x00
                });
                xpvbWriter.Write(compressGeometrie);
            }


            // XPVI-------------------------------------------
            byte[] xpvi = new byte[] { };
            using (BinaryDataWriter xpviWriter = new BinaryDataWriter(xpvi))
            {
                var compressTriangle = new LZ10().Compress(dataTriangle);
                xpviWriter.Write(new byte[] { 0x58, 0x50, 0x56, 0x49, 0x02, 0x00, 0x0C, 0x00 });
                xpviWriter.Write(Convert.ToInt32(dataTriangle.Length / 2));
                xpviWriter.Write(compressTriangle);
            }

            // Material-------------------------------------------
            byte[] material = new byte[] { };
            using (BinaryDataWriter materialWriter = new BinaryDataWriter(material))
            {
                materialWriter.Write(Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(meshName)));
                materialWriter.Write(Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(materialName)));
                materialWriter.Write(FromHex(mode[0]));
                materialWriter.Write(0);
                materialWriter.Write(0);
                materialWriter.Write(0);
                materialWriter.Write(0);
                materialWriter.Write(0);
                materialWriter.Write(0);
                materialWriter.Write(dimensions.X);
                materialWriter.Write(dimensions.Y);
                materialWriter.Write(dimensions.Z);
                materialWriter.Write(0);
                materialWriter.Write(FromHex(mode[1]));
                materialWriter.Write(boneNames.Count);

            }

            // Node ------------------------------------------
            byte[] node = new byte[] { };
            using (BinaryDataWriter nodeWriter = new BinaryDataWriter(node))
            {
                foreach (var name in boneNames)
                {
                    nodeWriter.Write(Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(name)));
                }
            }

            // Name ------------------------------------------
            byte[] xmprName = new byte[] { };
            using (BinaryDataWriter xmprNameWriter = new BinaryDataWriter(xmprName))
            {
                xmprNameWriter.Write(Encoding.GetEncoding("shift-jis").GetBytes(meshName));
                xmprNameWriter.Write(0);
                xmprNameWriter.Write(Encoding.GetEncoding("shift-jis").GetBytes(materialName));
                xmprNameWriter.Write(0);
            }

            // XMPR------------------------------------------
            byte[] xmpr = new byte[] { };
            using (BinaryDataWriter xmprWriter = new BinaryDataWriter(xmpr))
            {
                xmprWriter.Write(new byte[] { 0x58, 0x4D, 0x50, 0x52 });
                xmprWriter.Write(64);
                xmprWriter.Write(84 + xpvb.Length + xpvi.Length);
                xmprWriter.Write(0);

                for (int i = 0; i < 3; i++)
                {
                    xmprWriter.Write(84 + xpvb.Length + xpvi.Length + material.Length);
                    xmprWriter.Write(0);
                }

                xmprWriter.Write(84 + xpvb.Length + xpvi.Length + material.Length + node.Length);
                xmprWriter.Write(node.Count());
                xmprWriter.Write(84 + xpvb.Length + xpvi.Length + material.Length + node.Length + xmprName.Length);

                xmprWriter.Write(xmprName.Length + 1);
                xmprWriter.Write(84 + xpvb.Length + xpvi.Length + material.Length + node.Length + xmprName.Length + 4);
                xmprWriter.Write(materialName.Length + 1);

                xmprWriter.Write(new byte[] { 0x58, 0x50, 0x52, 0x4D });
                xmprWriter.Write(20);
                xmprWriter.Write(xpvb.Length);
                xmprWriter.Write(xpvb.Length + 20);
                xmprWriter.Write(xpvi.Length);

                xmprWriter.Write(xpvb);
                xmprWriter.Write(xpvi);
                xmprWriter.Write(material);
                xmprWriter.Write(node);
                xmprWriter.Write(xmprName);

                return xmpr.ToArray();
            }
        }

        public static byte[] FromHex(string hex)
        {
            var bytes = new List<byte>();
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes.Add(Convert.ToByte(hex.Substring(i, 2), 16));
            }
            return bytes.ToArray();
        }
    }
}
