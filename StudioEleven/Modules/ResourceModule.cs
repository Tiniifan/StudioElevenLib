#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using StudioElevenLib.Level5.Resource;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Resource.Types.Scene3D;

namespace StudioEleven.Modules
{
    /// <summary>
    /// Groups all Level-5 Resource (RES/XRES) commands: res-info, res-merge.
    /// Register this module in Program.cs.
    /// </summary>
    public class ResourceModule : IModule
    {
        public string Name => "resource";
        public string Description => "Level 5 Resource (RES / XRES) parsing and editing operations";

        public IReadOnlyList<ICommand> Commands { get; } = new List<ICommand>
        {
            new ResInfoCommand(),
            new ResMergeCommand(),
        };
    }

    #region Input Helpers

    /// <summary>
    /// Provides helper methods to read binary or text from disk/stdin seamlessly.
    /// </summary>
    internal static class ResourceInputHelper
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

    #region Commands

    /// <summary>
    /// Reads a RES or XRES binary file and outputs its entire structure as JSON.
    /// </summary>
    internal sealed class ResInfoCommand : ICommand
    {
        public string Name => "res-info";
        public string Description => "Read a RES/XRES file and output its structure as JSON";
        public string Help =>
            "Usage: exe res-info [file_path]\n" +
            "\n" +
            "  Reads : .res / .xres from [file_path] OR Base64-encoded file from stdin\n" +
            "  Writes: JSON string representing the resource elements\n" +
            "\n" +
            "  Example (Python):\n" +
            "    proc = subprocess.run(['exe', 'res-info', 'scene.res'], capture_output=True)\n" +
            "    info = json.loads(proc.stdout)";

        public void Execute(string[] args)
        {
            byte[] data = ResourceInputHelper.ReadBinary(args, 1);
            IResource resource = Resourcer.GetResource(data);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            Console.WriteLine(JsonSerializer.Serialize(resource.Items, options));
        }
    }

    /// <summary>
    /// Adds a new element or updates an existing element in a RES/XRES file.
    /// To make piping easy, the JSON payload itself must contain the original file in Base64.
    /// </summary>
    internal sealed class ResMergeCommand : ICommand
    {
        public string Name => "res-merge";
        public string Description => "Add or update a node in a RES/XRES file via JSON merge";
        public string Help =>
            "Usage: exe res-merge [json_file_path]\n" +
            "\n" +
            "  Reads : JSON containing the Base64 original file and update instructions.\n" +
            "  Writes: Base64-encoded updated .res / .xres file\n" +
            "\n" +
            "  Example JSON Payload:\n" +
            "  {\n" +
            "    \"OriginalFileBase64\": \"<base64_data>\",\n" +
            "    \"Magic\": \"RESC01\",\n" +
            "    \"ResType\": \"MaterialData\",\n" +
            "    \"Name\": \"Mat_Hero\",\n" +
            "    \"Data\": { \"MaterialDataName1\": \"Tex_Hero_New\" }\n" +
            "  }\n";

        public void Execute(string[] args)
        {
            string json = ResourceInputHelper.ReadText(args, 1);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
            var request = JsonSerializer.Deserialize<ResMergeRequest>(json, options)
                ?? throw new Exception("Invalid JSON for RES merge operation.");

            if (string.IsNullOrEmpty(request.OriginalFileBase64))
                throw new Exception("The JSON payload must include 'OriginalFileBase64'.");

            if (!Enum.TryParse<RESType>(request.ResType, true, out RESType resType))
                throw new Exception($"Unknown RESType: {request.ResType}");

            byte[] originalData = Convert.FromBase64String(request.OriginalFileBase64);
            IResource resource = Resourcer.GetResource(originalData);

            if (!resource.Items.ContainsKey(resType))
            {
                resource.Items[resType] = new List<RESElement>();
            }

            var elementList = resource.Items[resType];
            var existingElement = elementList.FirstOrDefault(e => e.Name == request.Name);

            Type targetType = existingElement?.GetType() ?? DetermineElementType(resource, resType);

            JsonObject targetNode;
            if (existingElement != null)
            {
                targetNode = (JsonObject)JsonSerializer.SerializeToNode(existingElement, targetType, options)!;
            }
            else
            {
                targetNode = new JsonObject();
                targetNode["Name"] = request.Name;
            }

            if (request.Data != null)
            {
                foreach (var prop in request.Data)
                {
                    targetNode[prop.Key] = prop.Value != null ? JsonNode.Parse(prop.Value.ToJsonString()) : null;
                }
            }

            var finalElement = (RESElement)targetNode.Deserialize(targetType, options)!;

            if (existingElement != null)
            {
                int index = elementList.IndexOf(existingElement);
                elementList[index] = finalElement;
            }
            else
            {
                elementList.Add(finalElement);
            }

            string magic = request.Magic ?? (resource.Name == "XRES" ? "XRES" : "RESC01");
            byte[] updatedData = resource.Save(magic);

            Console.Write(Convert.ToBase64String(updatedData));
        }

        private static Type DetermineElementType(IResource resource, RESType type)
        {
            if (resource.Items.TryGetValue(type, out var list) && list.Count > 0)
            {
                return list[0].GetType();
            }

            if (type == RESType.TextureData)
                return resource.Name == "XRES" ? typeof(XRESTextureData) : typeof(RESTextureData);

            if (type == RESType.MaterialData)
                return typeof(ResMaterialData);

            var assembly = typeof(RESElement).Assembly;
            var targetTypeName = $"RES{type}";
            var foundType = assembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, targetTypeName, StringComparison.OrdinalIgnoreCase));

            return foundType ?? typeof(RESElement);
        }

        private class ResMergeRequest
        {
            public string? OriginalFileBase64 { get; set; }
            public string? Magic { get; set; }
            public string ResType { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public JsonObject? Data { get; set; }
        }
    }

    #endregion
}