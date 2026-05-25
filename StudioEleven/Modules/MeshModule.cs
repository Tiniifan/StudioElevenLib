#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using StudioElevenLib.Level5.Mesh.XMPR;
using StudioElevenLib.Level5.Mesh.XPVB;
using StudioElevenLib.Level5.Mesh.XPVI;

namespace StudioEleven.Modules
{
    /// <summary>
    /// Groups all Mesh (XMPR / .prm) commands: info, encode-xpvi, encode-xpvb, build-prm.
    /// Register this module in Program.cs.
    /// </summary>
    public class MeshModule : IModule
    {
        public string Name => "mesh";
        public string Description => "Level 5 Mesh (XMPR / .prm) and buffer (XPVB/XPVI) operations";

        public IReadOnlyList<ICommand> Commands { get; } = new List<ICommand>
        {
            new MeshInfoCommand(),
            new EncodeXpviCommand(),
            new EncodeXpvbCommand(),
            new BuildPrmCommand(),
        };
    }

    #region Input Helpers

    /// <summary>
    /// Provides helper methods to read from disk or stdin seamlessly.
    /// </summary>
    internal static class MeshInputHelper
    {
        public static byte[] ReadBinary(string[] args, int pathIndex)
        {
            if (args.Length > pathIndex && File.Exists(args[pathIndex]))
            {
                return File.ReadAllBytes(args[pathIndex]);
            }

            string b64 = Console.In.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(b64))
                throw new Exception("No input provided via arguments or stdin.");

            return Convert.FromBase64String(b64);
        }

        public static string ReadText(string[] args, int pathIndex)
        {
            if (args.Length > pathIndex && File.Exists(args[pathIndex]))
            {
                return File.ReadAllText(args[pathIndex]);
            }

            string text = Console.In.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(text))
                throw new Exception("No JSON input provided via arguments or stdin.");

            return text;
        }
    }

    #endregion

    #region JSON Schemas & Mappers

    public class JsonVertex
    {
        public float[]? Position { get; set; }
        public float[]? Normal { get; set; }
        public float[]? UV0 { get; set; }
        public float[]? UV1 { get; set; }
        public float[]? Weights { get; set; }
        public float[]? BoneIndices { get; set; }
        public float[]? Color { get; set; }
    }

    internal static class VertexMapper
    {
        public static XPVBSupport.Vertex Map(JsonVertex v)
        {
            return new XPVBSupport.Vertex
            {
                Position = ToVector3(v.Position),
                Normal = ToVector3(v.Normal),
                UV0 = ToVector2(v.UV0),
                UV1 = ToVector2(v.UV1),
                Weights = ToVector4(v.Weights),
                BoneIndices = ToVector4(v.BoneIndices),
                Color = ToVector4(v.Color)
            };
        }

        public static JsonVertex Map(XPVBSupport.Vertex v)
        {
            return new JsonVertex
            {
                Position = new float[] { v.Position.X, v.Position.Y, v.Position.Z },
                Normal = new float[] { v.Normal.X, v.Normal.Y, v.Normal.Z },
                UV0 = new float[] { v.UV0.X, v.UV0.Y },
                UV1 = new float[] { v.UV1.X, v.UV1.Y },
                Weights = new float[] { v.Weights.X, v.Weights.Y, v.Weights.Z, v.Weights.W },
                BoneIndices = new float[] { v.BoneIndices.X, v.BoneIndices.Y, v.BoneIndices.Z, v.BoneIndices.W },
                Color = new float[] { v.Color.X, v.Color.Y, v.Color.Z, v.Color.W }
            };
        }

        private static Vector2 ToVector2(float[]? arr) => arr != null && arr.Length >= 2 ? new Vector2(arr[0], arr[1]) : default;
        private static Vector3 ToVector3(float[]? arr) => arr != null && arr.Length >= 3 ? new Vector3(arr[0], arr[1], arr[2]) : default;
        private static Vector4 ToVector4(float[]? arr) => arr != null && arr.Length >= 4 ? new Vector4(arr[0], arr[1], arr[2], arr[3]) : default;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Reads a .prm (XMPR) file from disk or stdin and outputs its properties as JSON.
    /// </summary>
    internal sealed class MeshInfoCommand : ICommand
    {
        public string Name => "mesh-info";
        public string Description => "Read a .prm file and output its metadata as JSON";
        public string Help =>
            "Usage: exe mesh-info [file_path]\n" +
            "\n" +
            "  Reads : .prm from [file_path] OR Base64-encoded .prm from stdin\n" +
            "  Writes: JSON string containing mesh information\n" +
            "\n" +
            "  Example (Python):\n" +
            "    proc = subprocess.run(['exe', 'mesh-info', 'model.prm'], capture_output=True)\n" +
            "    info = json.loads(proc.stdout)";

        public void Execute(string[] args)
        {
            byte[] data = MeshInputHelper.ReadBinary(args, 1);
            var xmpr = new XMPR(data);

            var jsonVertices = new List<JsonVertex>();
            if (xmpr.Vertices?.Vertices != null)
            {
                foreach (var v in xmpr.Vertices.Vertices)
                {
                    jsonVertices.Add(VertexMapper.Map(v));
                }
            }

            var info = new
            {
                xmpr.MeshName,
                xmpr.MaterialName,
                xmpr.SingleBind,
                xmpr.DrawPriority,
                xmpr.MeshType,
                NodesCount = xmpr.Nodes?.Count ?? 0,
                Nodes = xmpr.Nodes,
                Indices = xmpr.Triangles?.Triangles,
                Vertices = jsonVertices
            };

            Console.WriteLine(JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    /// <summary>
    /// Reads JSON containing triangle indices and format, and outputs the XPVI block as Base64.
    /// </summary>
    internal sealed class EncodeXpviCommand : ICommand
    {
        public string Name => "encode-xpvi";
        public string Description => "Create an XPVI block from JSON indices and format";
        public string Help =>
            "Usage: exe encode-xpvi [json_file_path]\n" +
            "\n" +
            "  Reads : JSON from [json_file_path] OR stdin\n" +
            "          Format: { \"Indices\": [0, 1, 2], \"Format\": \"Strip\" or \"List\" }\n" +
            "  Writes: Base64-encoded XPVI data block";

        public void Execute(string[] args)
        {
            string json = MeshInputHelper.ReadText(args, 1);
            var request = JsonSerializer.Deserialize<XpviRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Invalid JSON for XPVI encoding.");

            var xpvi = new XPVI();
            if (request.Indices != null)
            {
                xpvi.Triangles.AddRange(request.Indices);
            }

            bool useTriStrip = (request.Format ?? "Strip").Equals("Strip", StringComparison.OrdinalIgnoreCase);
            var writer = new XPVIWriter(xpvi, useTriStrip);
            byte[] xpviData = writer.Save();

            Console.Write(Convert.ToBase64String(xpviData));
        }

        private class XpviRequest
        {
            public List<ushort>? Indices { get; set; }
            public string? Format { get; set; }
        }
    }

    /// <summary>
    /// Reads JSON containing vertices data, and outputs the XPVB block as Base64.
    /// </summary>
    internal sealed class EncodeXpvbCommand : ICommand
    {
        public string Name => "encode-xpvb";
        public string Description => "Create an XPVB block from JSON vertices";
        public string Help =>
            "Usage: exe encode-xpvb [json_file_path]\n" +
            "\n" +
            "  Reads : JSON from [json_file_path] OR stdin containing vertex data\n" +
            "  Writes: Base64-encoded XPVB data block";

        public void Execute(string[] args)
        {
            string json = MeshInputHelper.ReadText(args, 1);
            var request = JsonSerializer.Deserialize<XpvbRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Invalid JSON for XPVB encoding.");

            var xpvb = new XPVB();
            if (request.Vertices != null)
            {
                foreach (var v in request.Vertices)
                {
                    xpvb.Vertices.Add(VertexMapper.Map(v));
                }
            }

            byte[] xpvbData = xpvb.Save();
            Console.Write(Convert.ToBase64String(xpvbData));
        }

        private class XpvbRequest
        {
            public List<JsonVertex>? Vertices { get; set; }
        }
    }

    /// <summary>
    /// Constructs a complete .prm (XMPR) file from a JSON object containing all mesh logic.
    /// </summary>
    internal sealed class BuildPrmCommand : ICommand
    {
        public string Name => "build-prm";
        public string Description => "Create a complete .prm (XMPR) from JSON definition";
        public string Help =>
            "Usage: exe build-prm [json_file_path]\n" +
            "\n" +
            "  Reads : JSON from file OR stdin containing all mesh properties, vertices, and indices.\n" +
            "  Writes: Base64-encoded .prm (XMPR) file";

        public void Execute(string[] args)
        {
            string json = MeshInputHelper.ReadText(args, 1);
            var req = JsonSerializer.Deserialize<BuildPrmRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Invalid JSON for PRM building.");

            var xmpr = new XMPR
            {
                MeshName = req.MeshName ?? "DefaultMesh",
                MaterialName = req.MaterialName ?? "DefaultMaterial",
                SingleBind = req.SingleBind,
                DrawPriority = req.DrawPriority,
                MeshType = req.MeshType,
                Nodes = req.Nodes ?? new List<uint>()
            };

            if (req.Indices != null)
            {
                xmpr.Triangles.Triangles.AddRange(req.Indices);
            }

            if (req.Vertices != null)
            {
                foreach (var v in req.Vertices)
                {
                    xmpr.Vertices.Vertices.Add(VertexMapper.Map(v));
                }
            }

            // Calls XMPRWriter internally which builds the final PRM from assigned Vertices and Triangles
            byte[] xmprData = xmpr.Save();

            Console.Write(Convert.ToBase64String(xmprData));
        }

        private class BuildPrmRequest
        {
            public string? MeshName { get; set; }
            public string? MaterialName { get; set; }
            public uint? SingleBind { get; set; }
            public uint DrawPriority { get; set; }
            public ushort MeshType { get; set; }
            public List<uint>? Nodes { get; set; }

            public List<ushort>? Indices { get; set; }
            public List<JsonVertex>? Vertices { get; set; }
        }
    }

    #endregion
}