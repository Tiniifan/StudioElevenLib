using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Mesh.Logic;

namespace StudioElevenLib.Level5.Mesh
{
    public static class XMPR
    {
        public static List<string> UsedBones(Dictionary<int, Dictionary<int, float>> weights, Dictionary<int, string> boneNames)
        {
            var used = new HashSet<string>();
            foreach (var key in weights.Keys)
            {
                foreach (var subKey in weights[key].Keys)
                {
                    // Utilisez boneNames[subKey] qui est de type string
                    string boneName = boneNames[subKey];

                    // Vérifiez si le nom de l'os est déjà dans l'ensemble
                    if (!used.Contains(boneName))
                    {
                        used.Add(boneName);
                    }
                }
            }
            return used.ToList();
        }

        public static Dictionary<int, Dictionary<int, float>> UsedWeights(Dictionary<int, Dictionary<int, float>> weights)
        {
            var used = new Dictionary<int, int>(); // Correction du type de used pour stocker un int pour chaque subKey
            var index = 0;

            foreach (var key in weights.Keys.ToList())
            {
                var tempDict = weights[key];
                weights[key] = new Dictionary<int, float>(); // Initialisation d'un nouveau dictionnaire pour chaque key

                foreach (var subKey in tempDict.Keys)
                {
                    // Si subKey n'est pas encore utilisé, on lui assigne un index
                    if (!used.ContainsKey(subKey))
                    {
                        used[subKey] = index;
                        index++;
                    }

                    // Utilisation de used[subKey] comme clé dans le dictionnaire weights[key]
                    weights[key][used[subKey]] = tempDict[subKey];
                }
            }

            return weights;
        }

        public static List<Dictionary<string, int>> RemoveDupeIndices(List<Dictionary<string, List<int>>> indices)
        {
            var usedIndices = new List<Tuple<float, float, float>>();
            var finalIndices = new List<Dictionary<string, int>>();
            var keys = new[] { "v", "vt", "vn", "vc" };

            foreach (var indice in indices)
            {
                for (int i = 0; i < 3; i++)
                {
                    var geometrie = new List<float>();
                    foreach (var key in indice.Keys)
                    {
                        if (indice[key].Count > i)
                            geometrie.Add(indice[key][i]);
                    }

                    var geometrieTuple = Tuple.Create(geometrie[0], geometrie[1], geometrie[2]);
                    if (!usedIndices.Contains(geometrieTuple))
                    {
                        usedIndices.Add(geometrieTuple);
                        var geometrieDict = new Dictionary<string, int>();
                        for (int j = 0; j < geometrie.Count; j++)
                        {
                            geometrieDict[keys[j]] = Convert.ToInt32(geometrie[j]);
                        }
                        finalIndices.Add(geometrieDict);
                    }
                }
            }

            return finalIndices;
        }

        public static byte[] WriteGeometrie(List<Dictionary<string, List<int>>> indices, Logic.Mesh mesh)
        {
            var outBytes = new List<byte>();
            indices = RemoveDupeIndices(indices);

            foreach (var indice in indices)
            {
                foreach (var v in mesh.Vertices[indice["v"]])
                    outBytes.AddRange(BitConverter.GetBytes(v));
                foreach (var n in mesh.Normals[indice["vn"]])
                    outBytes.AddRange(BitConverter.GetBytes(n));
                foreach (var vt in new[] { 0, 1 })
                {
                    outBytes.AddRange(BitConverter.GetBytes(mesh.UVs[indice["vt"]][0]));
                    outBytes.AddRange(BitConverter.GetBytes(1 - mesh.UVs[indice["vt"]][1]));
                }

                var weight = mesh.Weights[indice["v"]];
                var keys = weight.Keys.ToList();
                for (int w = 0; w < 4; w++)
                {
                    if (w < keys.Count)
                        outBytes.AddRange(BitConverter.GetBytes((float)weight[keys[w]]));
                    else
                        outBytes.AddRange(BitConverter.GetBytes(0f));
                }

                for (int x = 0; x < 4; x++)
                {
                    if (x < keys.Count)
                        outBytes.AddRange(BitConverter.GetBytes((float)keys[x]));
                    else
                        outBytes.AddRange(BitConverter.GetBytes(0f));
                }

                for (int c = 0; c < 4; c++)
                {
                    if (mesh.Colors.Count > 0)
                        outBytes.AddRange(BitConverter.GetBytes(mesh.Colors[indice["vc"]][c]));
                    else
                        outBytes.AddRange(BitConverter.GetBytes(0f));
                }
            }
            return outBytes.ToArray();
        }
    }
}
