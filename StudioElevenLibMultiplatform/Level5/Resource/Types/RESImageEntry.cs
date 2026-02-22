using System.Collections.Generic;

namespace StudioElevenLib.Level5.Resource.Types
{
    /// <summary>
    /// Represents the raw data structure of an image entry in a RES file.
    /// </summary>
    public struct RESImageEntryStruct
    {
        /// <summary>
        /// CRC32 hash of the image entry name.
        /// </summary>
        public uint NameCrc32 { get; set; }

        /// <summary>
        /// Indicates whether the image entry is enabled (0 = false, non-zero = true).
        /// </summary>
        public int Enabled { get; set; }

        public float Unk1 { get; set; }
        public float Unk2 { get; set; }
        public float Unk3 { get; set; }
        public float Unk4 { get; set; }
        public float Unk5 { get; set; }
        public float Unk6 { get; set; }
        public float Unk7 { get; set; }
        public float Unk8 { get; set; }
        public float Unk9 { get; set; }
        public float Unk10 { get; set; }
        public float Unk11 { get; set; }
    }

    /// <summary>
    /// Represents a parsed RES image entry with a resolved name,
    /// </summary>
    public class RESImageEntry
    {
        /// <summary>
        /// The resolved name of the image entry.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates whether the image entry is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        public float Unk1 { get; set; }
        public float Unk2 { get; set; }
        public float Unk3 { get; set; }
        public float Unk4 { get; set; }
        public float Unk5 { get; set; }
        public float Unk6 { get; set; }
        public float Unk7 { get; set; }
        public float Unk8 { get; set; }
        public float Unk9 { get; set; }
        public float Unk10 { get; set; }
        public float Unk11 { get; set; }

        /// <summary>
        /// Initializes a new empty instance of <see cref="RESImageEntry"/>.
        /// </summary>
        public RESImageEntry()
        {
        }

        /// <summary>
        /// Converts a raw <see cref="RESImageEntryStruct"/> into a parsed <see cref="RESImageEntry"/>.
        /// Resolves the name using the CRC32 value and the provided string table.
        /// </summary>
        /// <param name="imageStruct">The raw RES image entry structure.</param>
        /// <param name="stringTable">A dictionary mapping known names to their CRC32 hashes.</param>
        /// <returns>A populated <see cref="RESImageEntry"/> instance.</returns>
        public static RESImageEntry FromStruct(RESImageEntryStruct imageStruct, Dictionary<string, uint> stringTable)
        {
            string name = ResourceHelper.ResolveName(imageStruct.NameCrc32, stringTable);

            return new RESImageEntry
            {
                Name = name,
                Enabled = imageStruct.Enabled != 0,
                Unk1 = imageStruct.Unk1,
                Unk2 = imageStruct.Unk2,
                Unk3 = imageStruct.Unk3,
                Unk4 = imageStruct.Unk4,
                Unk5 = imageStruct.Unk5,
                Unk6 = imageStruct.Unk6,
                Unk7 = imageStruct.Unk7,
                Unk8 = imageStruct.Unk8,
                Unk9 = imageStruct.Unk9,
                Unk10 = imageStruct.Unk10,
                Unk11 = imageStruct.Unk11
            };
        }

        /// <summary>
        /// Converts this <see cref="RESImageEntry"/> into a <see cref="RESImageEntryStruct"/> using the string table.
        /// Throws an exception if the name is not found.
        /// </summary>
        /// <param name="stringTable">Dictionary mapping names to (CRC32, textOffset).</param>
        /// <returns>The raw <see cref="RESImageEntryStruct"/> corresponding to this instance.</returns>
        public RESImageEntryStruct ToStruct(Dictionary<string, (uint, int)> stringTable)
        {
            if (!stringTable.ContainsKey(Name))
                throw new KeyNotFoundException($"The name '{Name}' was not found in the string table.");

            var (crc32, offset) = stringTable[Name];

            return new RESImageEntryStruct
            {
                NameCrc32 = crc32,
                Enabled = Enabled ? 1 : 0,
                Unk1 = Unk1,
                Unk2 = Unk2,
                Unk3 = Unk3,
                Unk4 = Unk4,
                Unk5 = Unk5,
                Unk6 = Unk6,
                Unk7 = Unk7,
                Unk8 = Unk8,
                Unk9 = Unk9,
                Unk10 = Unk10,
                Unk11 = Unk11
            };
        }
    }
}