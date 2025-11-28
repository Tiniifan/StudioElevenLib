using System.Collections.Generic;

namespace StudioElevenLib.Level5.Resource.Types.Scene3D
{
    /// <summary>
    /// Represents the raw texture data structure for the modern RES format (post-2011).
    /// </summary>
    public struct RESTextureDataStruct
    {
        /// <summary>
        /// The base resource element structure containing crc32 of the name and name offset.
        /// </summary>
        public ResElementStruct ResElementStruct { get; set; }

        public int Unk1 { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }
    }

    /// <summary>
    /// Represents the raw texture data structure for the older XRES format (used around 2011).
    /// </summary>
    public struct XRESTextureDataStruct
    {
        /// <summary>
        /// The base resource element structure containing crc32 of the name and name offset.
        /// </summary>
        public ResElementStruct ResElementStruct { get; set; }

        public int Unk1 { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }
        public int Unk4 { get; set; }
        public int Unk5 { get; set; }
        public int Unk6 { get; set; }
    }

    /// <summary>
    /// Represents a parsed texture entry from the modern RES format.
    /// </summary>
    public class RESTextureData : RESElement
    {
        public int Unk1 { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RESTextureData"/>.
        /// </summary>
        public RESTextureData()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RESTextureData"/> with the specified values.
        /// </summary>
        /// <param name="name">Resolved resource name.</param>
        /// <param name="unk1">Unknown parameter #1.</param>
        /// <param name="unk2">Unknown parameter #2.</param>
        /// <param name="unk3">Unknown parameter #3.</param>
        public RESTextureData(string name, int unk1, int unk2, int unk3) : base(name)
        {
            Unk1 = unk1;
            Unk2 = unk2;
            Unk3 = unk3;
        }

        /// <summary>
        /// Converts a raw <see cref="RESTextureDataStruct"/> into a fully parsed <see cref="RESTextureData"/> instance.
        /// </summary>
        /// <param name="textureStruct">The raw structure from the RES resource.</param>
        /// <param name="stringTable">String-to-CRC32 lookup table for resolving names.</param>
        /// <returns>A populated <see cref="RESTextureData"/> object.</returns>
        public static RESTextureData FromStruct(RESTextureDataStruct textureStruct, Dictionary<string, uint> stringTable)
        {
            string name = ResourceHelper.ResolveName(textureStruct.ResElementStruct.NameCrc32, stringTable);

            return new RESTextureData
            {
                Name = name,
                Unk1 = textureStruct.Unk1,
                Unk2 = textureStruct.Unk2,
                Unk3 = textureStruct.Unk3
            };
        }
    }

    /// <summary>
    /// Represents a parsed texture entry from the legacy XRES format (circa 2011).
    /// </summary>
    public class XRESTextureData : RESElement
    {
        public int Unk1 { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }
        public int Unk4 { get; set; }
        public int Unk5 { get; set; }
        public int Unk6 { get; set; }

        /// <summary>
        /// Initializes a new empty instance of <see cref="XRESTextureData"/>.
        /// </summary>
        public XRESTextureData()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="XRESTextureData"/> with the specified values.
        /// </summary>
        /// <param name="name">Resolved resource name.</param>
        /// <param name="unk1">Unknown parameter #1.</param>
        /// <param name="unk2">Unknown parameter #2.</param>
        /// <param name="unk3">Unknown parameter #3.</param>
        /// <param name="unk4">Unknown parameter #4 (XRES-specific).</param>
        /// <param name="unk5">Unknown parameter #5 (XRES-specific).</param>
        /// <param name="unk6">Unknown parameter #6 (XRES-specific).</param>
        public XRESTextureData(string name, int unk1, int unk2, int unk3, int unk4, int unk5, int unk6) : base(name)
        {
            Unk1 = unk1;
            Unk2 = unk2;
            Unk3 = unk3;
            Unk4 = unk4;
            Unk5 = unk5;
            Unk6 = unk6;
        }

        /// <summary>
        /// Converts a raw <see cref="XRESTextureDataStruct"/> into a fully parsed <see cref="XRESTextureData"/> instance.
        /// </summary>
        /// <param name="textureStruct">The raw structure from the XRES resource.</param>
        /// <param name="stringTable">String-to-CRC32 lookup table for resolving names.</param>
        /// <returns>A populated <see cref="XRESTextureData"/> object.</returns>
        public static XRESTextureData FromStruct(XRESTextureDataStruct textureStruct, Dictionary<string, uint> stringTable)
        {
            string name = ResourceHelper.ResolveName(textureStruct.ResElementStruct.NameCrc32, stringTable);

            return new XRESTextureData
            {
                Name = name,
                Unk1 = textureStruct.Unk1,
                Unk2 = textureStruct.Unk2,
                Unk3 = textureStruct.Unk3,
                Unk4 = textureStruct.Unk4,
                Unk5 = textureStruct.Unk5,
                Unk6 = textureStruct.Unk6
            };
        }
    }
}