using System.Collections.Generic;
using StudioElevenLib.Tools;
using System.Text;
using System;

namespace StudioElevenLib.Level5.Resource.Types
{
    /// <summary>
    /// Represents the raw data structure of a resource element,
    /// containing the name CRC32 hash and the text offset.
    /// </summary>
    public struct ResElementStruct
    {
        /// <summary>
        /// Gets or sets the CRC32 hash of the element's name.
        /// </summary>
        public uint NameCrc32 { get; set; }

        /// <summary>
        /// Gets or sets the offset of the associated text inside the resource.
        /// </summary>
        public int TextOffset { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ResElementStruct"/> with the given CRC32 name hash and text offset.
        /// </summary>
        /// <param name="nameCrc32">CRC32 hash of the name.</param>
        /// <param name="textOffset">Offset of the text in the resource.</param>
        public ResElementStruct(uint nameCrc32, int textOffset)
        {
            NameCrc32 = nameCrc32;
            TextOffset = textOffset;
        }
    }

    /// <summary>
    /// Represents a higher-level resource element that stores the resolved name.
    /// </summary>
    public class RESElement
    {
        /// <summary>
        /// Gets or sets the resolved name of the element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new empty instance of <see cref="RESElement"/>.
        /// </summary>
        public RESElement()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RESElement"/> with the specified name.
        /// </summary>
        /// <param name="name">The resolved name of the element.</param>
        public RESElement(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates a <see cref="RESElement"/> from a <see cref="ResElementStruct"/> by resolving its name using the string table.
        /// </summary>
        /// <param name="elementStruct">The raw structure containing the CRC32 name hash.</param>
        /// <param name="stringTable">A dictionary of known strings mapped to their CRC32 hashes.</param>
        /// <returns>A new <see cref="RESElement"/> with a resolved name.</returns>
        public static RESElement FromStruct(ResElementStruct elementStruct, Dictionary<string, uint> stringTable)
        {
            string name = ResourceHelper.ResolveName(elementStruct.NameCrc32, stringTable);
            return new RESElement(name);
        }

        /// <summary>
        /// Converts this <see cref="RESElement"/> into a <see cref="ResElementStruct"/> using the string table.
        /// Throws an exception if the name is not present in the string table.
        /// </summary>
        /// <param name="stringTable">A dictionary mapping strings to their CRC32 and textOffset values.</param>
        /// <returns>A <see cref="ResElementStruct"/> representing this element.</returns>
        public ResElementStruct ToStruct(Dictionary<string, (uint, int)> stringTable)
        {
            if (!stringTable.ContainsKey(Name))
            {
                throw new KeyNotFoundException($"The name '{Name}' was not found in the string table.");
            }

            uint crc32 = stringTable[Name].Item1;
            int pos = stringTable[Name].Item2;

            Console.WriteLine(this + " " + Name + " " + crc32.ToString("X8") + " " + pos);

            return new ResElementStruct(crc32, pos);
        }
    }
}