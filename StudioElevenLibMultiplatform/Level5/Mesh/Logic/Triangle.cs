using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using SharpTriStrip;

namespace StudioElevenLib.Level5.Mesh.Logic
{
    public static class Triangle
    {
        public static List<int> IndiceToStrip(List<int[]> indices)
        {
            // Initialize TriStrip instance
            TriStrip triStripGenerator = new TriStrip();

            // Flatten all indices
            var allIndices = indices.SelectMany(x => x).ToArray();
            triStripGenerator.GenerateStrips(allIndices.Select(i => (ushort)i).ToArray(), out var primGroups);

            // Convert the generated strips into the desired format
            List<int> triangleStrip = new List<int>();
            foreach (var group in primGroups)
            {
                if (group.Type == TriStrip.PrimitiveType.Strip)
                {
                    triangleStrip.AddRange(group.Indices.Select(i => (int)i));
                }
            }

            return triangleStrip;
        }

        public static List<int> IndiceToStrip(List<Vector3> indices)
        {
            // Initialize TriStrip instance
            TriStrip triStripGenerator = new TriStrip();

            // Flatten all indices by taking each component of Vector3
            var allIndices = indices.SelectMany(v => new[] { (int)v.X, (int)v.Y, (int)v.Z }).ToArray();
            triStripGenerator.GenerateStrips(allIndices.Select(i => (ushort)i).ToArray(), out var primGroups);

            // Convert the generated strips into the desired format
            List<int> triangleStrip = new List<int>();
            foreach (var group in primGroups)
            {
                if (group.Type == TriStrip.PrimitiveType.Strip)
                {
                    triangleStrip.AddRange(group.Indices.Select(i => (int)i));
                }
            }

            return triangleStrip;
        }

        // Fonction qui convertit une liste de Triangle Strips en une liste d'indices de triangles
        public static List<int[]> StripToIndice(List<int> triangleStrip)
        {
            List<int[]> triangleIndicesList = new List<int[]>();

            // Assuming that TriangleStripIndices are organized in strips
            for (int i = 0; i < triangleStrip.Count - 2; i++)
            {
                // Generate triangles from strips (triplets of indices)
                if (i % 2 == 0) // Even indices form the first triangle
                {
                    triangleIndicesList.Add(new int[]
                    {
                        triangleStrip[i], triangleStrip[i + 1], triangleStrip[i + 2]
                    });
                }
                else // Odd indices form the second triangle
                {
                    triangleIndicesList.Add(new int[]
                    {
                        triangleStrip[i + 1], triangleStrip[i], triangleStrip[i + 2]
                    });
                }
            }

            return triangleIndicesList;
        }
    }
}
