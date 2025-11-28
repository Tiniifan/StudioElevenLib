using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Resource.Types.Scene3D
{
    /// <summary>
    /// Represents the raw material data structure in a RES Scene3D file.
    /// </summary>
    public struct ResMaterialDataStruct
    {
        /// <summary>
        /// The base resource element structure containing crc32 of the name and name offset.
        /// </summary>
        public ResElementStruct ResElementStruct { get; set; }

        /// <summary>
        /// CRC32 hash for the first material data name.
        /// </summary>
        public int MaterialDataName1Crc32 { get; set; }

        /// <summary>
        /// CRC32 hash for the second material data name.
        /// </summary>
        public int MaterialDataName2Crc32 { get; set; }

        /// <summary>
        /// First linked image entry.
        /// </summary>
        public RESImageEntryStruct Image1 { get; set; }

        /// <summary>
        /// Second linked image entry.
        /// </summary>
        public RESImageEntryStruct Image2 { get; set; }

        /// <summary>
        /// Third linked image entry.
        /// </summary>
        public RESImageEntryStruct Image3 { get; set; }

        /// <summary>
        /// Fourth linked image entry.
        /// </summary>
        public RESImageEntryStruct Image4 { get; set; }
    }

    /// <summary>
    /// Represents a parsed material data entry for Scene3D resources.
    /// </summary>
    public class ResMaterialData : RESElement
    {
        /// <summary>
        /// The resolved first material data name.
        /// </summary>
        public string MaterialDataName1 { get; set; }

        /// <summary>
        /// The resolved second material data name.
        /// </summary>
        public string MaterialDataName2 { get; set; }

        /// <summary>
        /// The collection of four image entries linked to this material.
        /// </summary>
        public RESImageEntry[] Images { get; set; } = new RESImageEntry[4];

        /// <summary>
        /// Initializes a new empty instance of <see cref="ResMaterialData"/>.
        /// </summary>
        public ResMaterialData()
        {
        }

        /// <summary>
        /// Converts a raw <see cref="ResMaterialDataStruct"/> into a parsed <see cref="ResMaterialData"/>.
        /// Resolves all CRC32 name values and converts image entries.
        /// </summary>
        /// <param name="materialStruct">The raw material data structure.</param>
        /// <param name="stringTable">A dictionary mapping known strings to CRC32 hashes.</param>
        /// <returns>A populated <see cref="ResMaterialData"/> instance.</returns>
        public static ResMaterialData FromStruct(ResMaterialDataStruct materialStruct, Dictionary<string, uint> stringTable)
        {
            string name = ResourceHelper.ResolveName(materialStruct.ResElementStruct.NameCrc32, stringTable);
            string materialName1 = ResourceHelper.ResolveName(materialStruct.MaterialDataName1Crc32, stringTable);
            string materialName2 = ResourceHelper.ResolveName(materialStruct.MaterialDataName2Crc32, stringTable);

            return new ResMaterialData
            {
                Name = name,
                MaterialDataName1 = materialName1,
                MaterialDataName2 = materialName2,
                Images = new RESImageEntry[]
                {
                    RESImageEntry.FromStruct(materialStruct.Image1, stringTable),
                    RESImageEntry.FromStruct(materialStruct.Image2, stringTable),
                    RESImageEntry.FromStruct(materialStruct.Image3, stringTable),
                    RESImageEntry.FromStruct(materialStruct.Image4, stringTable)
                }
            };
        }
    }
}