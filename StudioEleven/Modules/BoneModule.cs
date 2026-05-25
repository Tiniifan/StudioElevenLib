#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using StudioElevenLib.Level5.Armature;
using StudioElevenLib.Level5.Armature.Logic;

namespace StudioEleven.Modules
{
    /// <summary>
    /// Groups all Level-5 Bone (MBN) commands: mbn-info, build-mbn.
    /// Register this module in Program.cs.
    /// </summary>
    public class BoneModule : IModule
    {
        public string Name => "bone";
        public string Description => "Level 5 Bone (MBN) encoding and decoding operations";

        public IReadOnlyList<ICommand> Commands { get; } = new List<ICommand>
        {
            new MbnInfoCommand(),
            new BuildMbnCommand(),
        };
    }

    #region Input Helpers

    /// <summary>
    /// Provides helper methods to read binary or text from disk/stdin seamlessly.
    /// </summary>
    internal static class BoneInputHelper
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

    /// <summary>
    /// Recursive JSON schema representing a Bone and its hierarchy.
    /// </summary>
    public class JsonBone
    {
        public string? Name { get; set; }
        public bool UseDeform { get; set; }
        public float[]? Location { get; set; }
        public float[]? Rotation { get; set; }
        public float[]? Scale { get; set; }
        public float[]? Head { get; set; }
        public float[]? Tail { get; set; }

        public JsonBone? Parent { get; set; }
    }

    /// <summary>
    /// Handles bidirectional mapping between the library's Bone object and JsonBone.
    /// </summary>
    internal static class BoneMapper
    {
        public static Bone? Map(JsonBone? jb)
        {
            if (jb == null) return null;

            Bone? parent = Map(jb.Parent);
            Vector3 loc = ToVector3(jb.Location) ?? Vector3.Zero;
            Quaternion rot = ToQuaternion(jb.Rotation) ?? Quaternion.Identity;
            Vector3 scale = ToVector3(jb.Scale) ?? Vector3.One;

            var bone = new Bone(jb.Name ?? "unnamed", loc, rot, scale, jb.UseDeform, parent);

            if (jb.Head != null && jb.Head.Length >= 3)
                bone.Head = ToVector3(jb.Head)!.Value;

            if (jb.Tail != null && jb.Tail.Length >= 3)
                bone.Tail = ToVector3(jb.Tail)!.Value;

            return bone;
        }

        public static JsonBone? MapToJson(Bone? bone)
        {
            if (bone == null) return null;

            return new JsonBone
            {
                Name = bone.Name,
                UseDeform = bone.UseDeform,
                Location = new[] { bone.Location.X, bone.Location.Y, bone.Location.Z },
                Rotation = new[] { bone.Rotation.X, bone.Rotation.Y, bone.Rotation.Z, bone.Rotation.W },
                Scale = new[] { bone.Scale.X, bone.Scale.Y, bone.Scale.Z },
                Head = new[] { bone.Head.X, bone.Head.Y, bone.Head.Z },
                Tail = new[] { bone.Tail.X, bone.Tail.Y, bone.Tail.Z },
                Parent = MapToJson(bone.Parent)
            };
        }

        private static Vector3? ToVector3(float[]? arr) =>
            arr != null && arr.Length >= 3 ? new Vector3(arr[0], arr[1], arr[2]) : null;

        private static Quaternion? ToQuaternion(float[]? arr) =>
            arr != null && arr.Length >= 4 ? new Quaternion(arr[0], arr[1], arr[2], arr[3]) : null;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Reads an MBN binary file from disk or stdin and outputs the parsed Bone hierarchy as JSON.
    /// </summary>
    internal sealed class MbnInfoCommand : ICommand
    {
        public string Name => "mbn-info";
        public string Description => "Read an MBN file and output its Bone structure as JSON";
        public string Help =>
            "Usage: exe mbn-info [file_path]\n" +
            "\n" +
            "  Reads : .mbn from [file_path] OR Base64-encoded .mbn from stdin\n" +
            "  Writes: JSON string representing the Bone hierarchy\n" +
            "\n" +
            "  Example (Python):\n" +
            "    proc = subprocess.run(['exe', 'mbn-info', 'armature.mbn'], capture_output=True)\n" +
            "    info = json.loads(proc.stdout)";

        public void Execute(string[] args)
        {
            byte[] data = BoneInputHelper.ReadBinary(args, 1);
            var mbn = new MBN(data);

            if (mbn.Bone == null)
                throw new Exception("The provided MBN data does not contain a valid Bone.");

            JsonBone? jsonOutput = BoneMapper.MapToJson(mbn.Bone);
            Console.WriteLine(JsonSerializer.Serialize(jsonOutput, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    /// <summary>
    /// Constructs a complete MBN binary file from a JSON representation of a Bone.
    /// </summary>
    internal sealed class BuildMbnCommand : ICommand
    {
        public string Name => "build-mbn";
        public string Description => "Create an MBN binary from a JSON Bone definition";
        public string Help =>
            "Usage: exe build-mbn [json_file_path]\n" +
            "\n" +
            "  Reads : JSON from file OR stdin containing the Bone and its Parent hierarchy.\n" +
            "  Writes: Base64-encoded .mbn file\n" +
            "\n" +
            "  Example (Python):\n" +
            "    proc = subprocess.run(['exe', 'build-mbn'], input=bone_json, capture_output=True)\n" +
            "    with open('out.mbn', 'wb') as f: f.write(base64.b64decode(proc.stdout))";

        public void Execute(string[] args)
        {
            string json = BoneInputHelper.ReadText(args, 1);
            var request = JsonSerializer.Deserialize<JsonBone>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Invalid JSON for MBN building.");

            Bone bone = BoneMapper.Map(request)
                ?? throw new Exception("Could not map the provided JSON to a Bone object.");

            var mbn = new MBN(bone);
            byte[] mbnData = mbn.Save();

            Console.Write(Convert.ToBase64String(mbnData));
        }
    }

    #endregion
}