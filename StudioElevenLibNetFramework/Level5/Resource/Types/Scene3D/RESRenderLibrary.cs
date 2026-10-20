using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Resource.Types.Scene3D
{
    /// <summary>
    /// An entry of the render library kept as it is: its name and the bytes after the name hash and the string offset.
    /// </summary>
    public class RESLibraryEntry
    {
        public string Name { get; set; }

        public byte[] Data { get; set; }

        public RESLibraryEntry(string name, byte[] data)
        {
            Name = name;
            Data = data ?? new byte[0];
        }
    }

    /// <summary>
    /// A render program (render default): the vertex program and the combiner a mesh is drawn with.
    /// The game looks both up in the library of the archive first, then in its fix.xr.
    /// </summary>
    public class RESRenderProgram
    {
        public string Name { get; set; }

        public string VertexProgram { get; set; }

        public string Combiner { get; set; }

        public RESRenderProgram(string name, string vertexProgram, string combiner)
        {
            Name = name;
            VertexProgram = vertexProgram;
            Combiner = combiner;
        }
    }

    /// <summary>
    /// The render library a model archive can hold like fix.xr: shader binaries (SHDnnn.shdc), vertex programs,
    /// combiners (NNN.cmb), LUT (NNN.lut) and render programs. The numbered files follow the order of their table.
    /// </summary>
    public class RESRenderLibrary
    {
        // Types of the V2 library list, written in increasing order before the material tables
        public const short TypeShaderBinary = 100;
        public const short TypeVertexProgram = 110;
        public const short TypeCombiner = 120;
        public const short TypeRenderProgram = 190;
        public const short TypeLut = 200;

        // Slots of the V1 XRES header ({offset, count} pairs of 4 bytes)
        public const int SlotShaderBinary = 2;
        public const int SlotVertexProgram = 3;
        public const int SlotCombiner = 4;
        public const int SlotLut = 5;
        public const int SlotRenderProgram = 10;

        public static readonly short[] Types = { TypeShaderBinary, TypeVertexProgram, TypeCombiner, TypeRenderProgram, TypeLut };

        public List<RESLibraryEntry> ShaderBinaries { get; set; } = new List<RESLibraryEntry>();

        public List<RESLibraryEntry> VertexPrograms { get; set; } = new List<RESLibraryEntry>();

        public List<RESLibraryEntry> Combiners { get; set; } = new List<RESLibraryEntry>();

        public List<RESRenderProgram> RenderPrograms { get; set; } = new List<RESRenderProgram>();

        public List<RESLibraryEntry> Luts { get; set; } = new List<RESLibraryEntry>();

        public bool IsEmpty => ShaderBinaries.Count + VertexPrograms.Count + Combiners.Count + RenderPrograms.Count + Luts.Count == 0;

        public static bool IsLibraryType(short type)
        {
            return Types.Contains(type);
        }

        public static int EntryLength(short type)
        {
            switch (type)
            {
                case TypeShaderBinary: return 12;
                case TypeVertexProgram: return 20;
                case TypeRenderProgram: return 16;
                default: return 8;
            }
        }

        /// <summary>
        /// Reads one entry of a library table, a hash missing from the string table is kept as 0xXXXXXXXX.
        /// </summary>
        public void ReadEntry(short type, byte[] entry, Dictionary<string, uint> stringTable)
        {
            uint hash = BitConverter.ToUInt32(entry, 0);

            // The shader binaries have no name
            string name = hash == 0 ? "" : ResourceHelper.ResolveName(hash, stringTable);
            byte[] data = entry.Skip(8).ToArray();

            switch (type)
            {
                case TypeShaderBinary:
                    ShaderBinaries.Add(new RESLibraryEntry(name, data));
                    break;
                case TypeVertexProgram:
                    VertexPrograms.Add(new RESLibraryEntry(name, data));
                    break;
                case TypeCombiner:
                    Combiners.Add(new RESLibraryEntry(name, data));
                    break;
                case TypeLut:
                    Luts.Add(new RESLibraryEntry(name, data));
                    break;
                case TypeRenderProgram:
                    string vertexProgram = ResourceHelper.ResolveName(BitConverter.ToUInt32(entry, 8), stringTable);
                    string combiner = ResourceHelper.ResolveName(BitConverter.ToUInt32(entry, 12), stringTable);
                    RenderPrograms.Add(new RESRenderProgram(name, vertexProgram, combiner));
                    break;
            }
        }

        /// <summary>
        /// The names the library writes in the string table, the ones of fix.xr are kept so that a reader shows them.
        /// </summary>
        public IEnumerable<string> GetNames()
        {
            foreach (var entry in ShaderBinaries.Concat(VertexPrograms).Concat(Combiners).Concat(Luts))
            {
                yield return entry.Name;
            }

            foreach (var program in RenderPrograms)
            {
                yield return program.Name;
                yield return program.VertexProgram;
                yield return program.Combiner;
            }
        }

        /// <summary>
        /// The entries of each table in increasing type order, empty tables are left out.
        /// </summary>
        public List<(short type, List<byte[]> entries)> BuildSections(Dictionary<string, (uint crc32, int position)> stringDict)
        {
            var sections = new List<(short, List<byte[]>)>();

            AddSection(sections, TypeShaderBinary, ShaderBinaries.Select(e => BuildEntry(e.Name, e.Data, TypeShaderBinary, stringDict)));
            AddSection(sections, TypeVertexProgram, VertexPrograms.Select(e => BuildEntry(e.Name, e.Data, TypeVertexProgram, stringDict)));
            AddSection(sections, TypeCombiner, Combiners.Select(e => BuildEntry(e.Name, e.Data, TypeCombiner, stringDict)));
            AddSection(sections, TypeRenderProgram, RenderPrograms.Select(p => BuildEntry(p.Name,
                BitConverter.GetBytes(NameHash(p.VertexProgram)).Concat(BitConverter.GetBytes(NameHash(p.Combiner))).ToArray(),
                TypeRenderProgram, stringDict)));
            AddSection(sections, TypeLut, Luts.Select(e => BuildEntry(e.Name, e.Data, TypeLut, stringDict)));

            return sections;
        }

        private static void AddSection(List<(short, List<byte[]>)> sections, short type, IEnumerable<byte[]> entries)
        {
            List<byte[]> list = entries.ToList();

            if (list.Count > 0)
            {
                sections.Add((type, list));
            }
        }

        private static byte[] BuildEntry(string name, byte[] data, short type, Dictionary<string, (uint crc32, int position)> stringDict)
        {
            byte[] entry = new byte[EntryLength(type)];

            if (!string.IsNullOrEmpty(name))
            {
                Array.Copy(BitConverter.GetBytes(NameHash(name)), 0, entry, 0, 4);

                if (stringDict.ContainsKey(name))
                {
                    Array.Copy(BitConverter.GetBytes(stringDict[name].position), 0, entry, 4, 4);
                }
            }

            Array.Copy(data, 0, entry, 8, Math.Min(data.Length, entry.Length - 8));

            return entry;
        }

        /// <summary>
        /// The hash of a name, a name the archive didn't keep (0xXXXXXXXX) is written back as its hash.
        /// </summary>
        public static uint NameHash(string name)
        {
            if (IsHashName(name))
            {
                return Convert.ToUInt32(name.Substring(2), 16);
            }

            return Crc32.Compute(Encoding.GetEncoding(932).GetBytes(name));
        }

        public static bool IsHashName(string name)
        {
            return name != null && name.Length == 10 && name.StartsWith("0x");
        }
    }
}
