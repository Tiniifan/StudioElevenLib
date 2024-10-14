using System;
using System.Numerics;
using System.Collections.Generic;

namespace StudioElevenLib.Level5.Mesh.Logic
{
    public class Mesh
    {
        public string Name { get; set; }
        public List<Vector3> Vertices { get; set; }
        public List<Vector3> Normals { get; set; }
        public List<Vector2> UVs { get; set; }
        public List<Vector3> Colors { get; set; }
        public Dictionary<int, Dictionary<int, float>> Weights { get; set; }
        public List<int[]> Indices { get; set; }
        public List<int> TriangleStripIndices { get; set; }
        public bool IsListIndices { get; set; }

        public Mesh()
        {
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            UVs = new List<Vector2>();
            Colors = new List<Vector3>();
            Weights = new Dictionary<int, Dictionary<int, float>>();
            Indices = new List<int[]>();
            TriangleStripIndices = new List<int>();
            IsListIndices = true;
        }
    }
}
