using System.Collections.Generic;

namespace StudioElevenLib.Level5.Resource.Types.Scene3D
{
    /// <summary>
    /// How the GPU repeats a texture past its edges.
    /// </summary>
    public enum TextureWrapMode
    {
        Clamp = 0,
        Border = 1,
        Repeat = 2,
        Mirror = 3
    }

    /// <summary>
    /// How the GPU samples a texture.
    /// </summary>
    public enum TextureFilterMode
    {
        Nearest = 0,
        Linear = 1
    }

    /// <summary>
    /// The PICA200 sampler settings packed in the two bytes following the name offset of a texture entry,
    /// the first one holding the filters and the second one the wrap modes. Both the RES and the XRES
    /// containers store and read them the same way.
    /// </summary>
    public struct TextureSampler
    {
        public TextureWrapMode WrapS { get; set; }
        public TextureWrapMode WrapT { get; set; }
        public TextureFilterMode MagFilter { get; set; }
        public TextureFilterMode MinFilter { get; set; }
        public TextureFilterMode MipFilter { get; set; }

        /// <summary>
        /// The sampler every texture exported without any specific setting uses.
        /// </summary>
        public static TextureSampler Default => new TextureSampler
        {
            WrapS = TextureWrapMode.Repeat,
            WrapT = TextureWrapMode.Repeat,
            MagFilter = TextureFilterMode.Linear,
            MinFilter = TextureFilterMode.Linear,
            MipFilter = TextureFilterMode.Nearest
        };

        /// <summary>
        /// Decodes the filter and wrap bytes into a <see cref="TextureSampler"/>.
        /// </summary>
        /// <param name="filterByte">The byte holding the three filter flags.</param>
        /// <param name="wrapByte">The byte holding the two wrap modes.</param>
        /// <returns>The decoded sampler.</returns>
        public static TextureSampler Decode(byte filterByte, byte wrapByte)
        {
            return new TextureSampler
            {
                WrapS = (TextureWrapMode)(wrapByte & 3),
                WrapT = (TextureWrapMode)((wrapByte >> 2) & 3),
                MagFilter = (TextureFilterMode)(filterByte & 1),
                MinFilter = (TextureFilterMode)((filterByte >> 1) & 1),
                MipFilter = (TextureFilterMode)((filterByte >> 2) & 1)
            };
        }

        /// <summary>
        /// Encodes this sampler back into its filter and wrap bytes.
        /// </summary>
        /// <param name="filterByte">The byte holding the three filter flags.</param>
        /// <param name="wrapByte">The byte holding the two wrap modes.</param>
        public void Encode(out byte filterByte, out byte wrapByte)
        {
            filterByte = (byte)(((int)MagFilter & 1) | (((int)MinFilter & 1) << 1) | (((int)MipFilter & 1) << 2));
            wrapByte = (byte)(((int)WrapS & 3) | (((int)WrapT & 3) << 2));
        }

        /// <summary>
        /// Decodes the sampler out of the int following the name offset of a texture entry.
        /// </summary>
        /// <param name="packed">The int holding the filter byte, the wrap byte and two unused ones.</param>
        /// <returns>The decoded sampler.</returns>
        public static TextureSampler FromPacked(int packed)
        {
            return Decode((byte)(packed & 0xFF), (byte)((packed >> 8) & 0xFF));
        }

        /// <summary>
        /// Writes this sampler into the int following the name offset of a texture entry, leaving the two
        /// bytes the sampler doesn't use untouched.
        /// </summary>
        /// <param name="packed">The int to update.</param>
        /// <returns>The updated int.</returns>
        public int ToPacked(int packed)
        {
            Encode(out byte filterByte, out byte wrapByte);

            return (int)((packed & 0xFFFF0000) | (uint)(wrapByte << 8) | filterByte);
        }
    }

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
        /// The sampler settings held by the low bytes of <see cref="Unk1"/>.
        /// </summary>
        public TextureSampler Sampler
        {
            get => TextureSampler.FromPacked(Unk1);
            set => Unk1 = value.ToPacked(Unk1);
        }

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

        /// <summary>
        /// Converts this <see cref="RESTextureData"/> into a <see cref="RESTextureDataStruct"/> using the string table.
        /// Throws an exception if the name is not present in the table.
        /// </summary>
        /// <param name="stringTable">A dictionary mapping strings to (CRC32, textOffset) values.</param>
        /// <returns>The raw <see cref="RESTextureDataStruct"/> corresponding to this instance.</returns>
        public new RESTextureDataStruct ToStruct(Dictionary<string, (uint, int)> stringTable)
        {
            if (!stringTable.ContainsKey(Name))
                throw new KeyNotFoundException($"The name '{Name}' was not found in the string table.");

            var (crc32, offset) = stringTable[Name];

            return new RESTextureDataStruct
            {
                ResElementStruct = new ResElementStruct(crc32, offset),
                Unk1 = Unk1,
                Unk2 = Unk2,
                Unk3 = Unk3
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
        /// The sampler settings held by the low bytes of <see cref="Unk1"/>.
        /// </summary>
        public TextureSampler Sampler
        {
            get => TextureSampler.FromPacked(Unk1);
            set => Unk1 = value.ToPacked(Unk1);
        }

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

        /// <summary>
        /// Converts this <see cref="XRESTextureData"/> into a <see cref="XRESTextureDataStruct"/> using the string table.
        /// Throws an exception if the name is not present in the table.
        /// </summary>
        /// <param name="stringTable">A dictionary mapping strings to (CRC32, textOffset) values.</param>
        /// <returns>The raw <see cref="XRESTextureDataStruct"/> corresponding to this instance.</returns>
        public new XRESTextureDataStruct ToStruct(Dictionary<string, (uint, int)> stringTable)
        {
            if (!stringTable.ContainsKey(Name))
                throw new KeyNotFoundException($"The name '{Name}' was not found in the string table.");

            var (crc32, offset) = stringTable[Name];

            return new XRESTextureDataStruct
            {
                ResElementStruct = new ResElementStruct(crc32, offset),
                Unk1 = Unk1,
                Unk2 = Unk2,
                Unk3 = Unk3,
                Unk4 = Unk4,
                Unk5 = Unk5,
                Unk6 = Unk6
            };
        }
    }
}