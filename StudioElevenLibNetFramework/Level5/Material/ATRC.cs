using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.NoCompression;
using static StudioElevenLib.Level5.Material.ATRCSupport;

namespace StudioElevenLib.Level5.Material
{
    /// <summary>
    /// Render state of one material. A null property means the field is inherited.
    /// </summary>
    public class ATRC
    {
        public int FileVersion { get; set; }

        public bool? Cull { get; set; }

        public bool? DepthTest { get; set; }

        public bool? DepthWrite { get; set; }

        public CompareFunction? DepthFunction { get; set; }

        public bool? DepthBiasEnable { get; set; }

        public float? DepthBias { get; set; }

        public bool? AlphaTest { get; set; }

        public CompareFunction? AlphaFunction { get; set; }

        public float? AlphaReference { get; set; }

        public bool? Blend { get; set; }

        public BlendEquation? BlendRgbEquation { get; set; }

        public BlendFactor? BlendRgbSource { get; set; }

        public BlendFactor? BlendRgbDestination { get; set; }

        public BlendEquation? BlendAlphaEquation { get; set; }

        public BlendFactor? BlendAlphaSource { get; set; }

        public BlendFactor? BlendAlphaDestination { get; set; }

        /// <summary>
        /// Red, green, blue and alpha write masks, applied together or not at all.
        /// </summary>
        public bool[] ColorMask { get; set; }

        public bool? StencilTest { get; set; }

        public CompareFunction? StencilFunction { get; set; }

        public int? StencilReference { get; set; }

        public int? StencilCompareMask { get; set; }

        public int? StencilWriteMask { get; set; }

        // The meaning of the stencil operation values is undocumented, they stay raw numbers
        public int? StencilFailOperation { get; set; }

        public int? StencilDepthFailOperation { get; set; }

        public int? StencilDepthPassOperation { get; set; }

        public ATRC()
        {
            FileVersion = 2;
        }

        public ATRC(byte[] data)
        {
            FileVersion = 2;

            int dataOffset;

            if (data.Length >= LegacyHeaderSize && Encoding.ASCII.GetString(data, 0, 4) == "XATR")
            {
                // Legacy container, always V1, still used by a few models
                FileVersion = 1;
                dataOffset = BitConverter.ToUInt16(data, 4);

                if (dataOffset == 0)
                {
                    dataOffset = LegacyHeaderSize;
                }
            }
            else
            {
                if (data.Length < HeaderSize)
                {
                    throw new InvalidDataException("The ATR file is too small");
                }

                using (BinaryDataReader reader = new BinaryDataReader(data))
                {
                    ATRCHeader header = reader.ReadStruct<ATRCHeader>();

                    if (!int.TryParse(Encoding.ASCII.GetString(data, 4, 2), out int version) || version > 1)
                    {
                        throw new NotSupportedException($"Unsupported ATR version {Encoding.ASCII.GetString(data, 0, 6)}");
                    }

                    FileVersion = version + 1;
                    dataOffset = header.DataOffset == 0 ? HeaderSize : header.DataOffset;
                }
            }

            byte[] payload = Compressor.Decompress(data.Skip(dataOffset).ToArray());

            if (FileVersion == 1)
            {
                ReadV1(payload);
            }
            else
            {
                ReadV2(payload);
            }
        }

        public byte[] Save()
        {
            return Save(FileVersion);
        }

        public byte[] Save(int fileVersion)
        {
            byte[] payload;
            string version;

            if (fileVersion == 1)
            {
                version = "00";
                payload = WriteV1();
            }
            else if (fileVersion == 2)
            {
                version = "01";
                payload = WriteV2();
            }
            else
            {
                throw new NotSupportedException($"Unsupported ATR file version: {fileVersion}");
            }

            using (MemoryStream fileStream = new MemoryStream())
            {
                BinaryDataWriter writer = new BinaryDataWriter(fileStream);

                writer.Write(Encoding.ASCII.GetBytes("ATRC" + version));
                writer.Write((short)0);
                writer.Write((ushort)HeaderSize);
                writer.Write((ushort)0);
                writer.Write(new NoCompression().Compress(payload));

                return fileStream.ToArray();
            }
        }

        private static uint[] ToUInt32Array(byte[] payload, int size)
        {
            byte[] buffer = Enumerable.Repeat((byte)0xFF, size).ToArray();
            Array.Copy(payload, buffer, Math.Min(payload.Length, size));

            uint[] values = new uint[size / 4];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = BitConverter.ToUInt32(buffer, i * 4);
            }

            return values;
        }

        private void ReadV1(byte[] payload)
        {
            uint[] values = ToUInt32Array(payload, V1PayloadSize);

            Cull = IsInherited(values[0]) ? (bool?)null : values[0] != 0;
            DepthWrite = IsInherited(values[1]) ? (bool?)null : values[1] != 0;
            DepthTest = IsInherited(values[2]) ? (bool?)null : values[2] != 0;
            DepthFunction = IsInherited(values[3]) ? (CompareFunction?)null : (CompareFunction)values[3];
            AlphaFunction = IsInherited(values[5]) ? (CompareFunction?)null : (CompareFunction)values[5];
            BlendRgbEquation = IsInherited(values[7]) ? (BlendEquation?)null : (BlendEquation)values[7];
            BlendRgbSource = IsInherited(values[8]) ? (BlendFactor?)null : (BlendFactor)values[8];
            BlendRgbDestination = IsInherited(values[9]) ? (BlendFactor?)null : (BlendFactor)values[9];
            BlendAlphaEquation = IsInherited(values[10]) ? (BlendEquation?)null : (BlendEquation)values[10];
            BlendAlphaSource = IsInherited(values[11]) ? (BlendFactor?)null : (BlendFactor)values[11];
            BlendAlphaDestination = IsInherited(values[12]) ? (BlendFactor?)null : (BlendFactor)values[12];

            ColorMask = null;

            if (!IsInherited(values[13]) && !IsInherited(values[14]) && !IsInherited(values[15]) && !IsInherited(values[16]))
            {
                ColorMask = new[] { values[13] != 0, values[14] != 0, values[15] != 0, values[16] != 0 };
            }

            // Ints 4 and 6 can't be inherited, the loader forces them to 0 when they are missing
            DepthBias = BitConverter.ToSingle(BitConverter.GetBytes(values[4]), 0);
            AlphaReference = BitConverter.ToSingle(BitConverter.GetBytes(values[6]), 0);
        }

        private byte[] WriteV1()
        {
            uint[] values = Enumerable.Repeat(Inherit32, V1PayloadSize / 4).ToArray();

            values[0] = Cull.HasValue ? (Cull.Value ? 1u : 0u) : Inherit32;
            values[1] = DepthWrite.HasValue ? (DepthWrite.Value ? 1u : 0u) : Inherit32;
            values[2] = DepthTest.HasValue ? (DepthTest.Value ? 1u : 0u) : Inherit32;
            values[3] = DepthFunction.HasValue ? (uint)DepthFunction.Value : Inherit16;
            values[5] = AlphaFunction.HasValue ? (uint)AlphaFunction.Value : Inherit16;
            values[7] = BlendRgbEquation.HasValue ? (uint)BlendRgbEquation.Value : Inherit16;
            values[8] = BlendRgbSource.HasValue ? (uint)BlendRgbSource.Value : Inherit32;
            values[9] = BlendRgbDestination.HasValue ? (uint)BlendRgbDestination.Value : Inherit32;
            values[10] = BlendAlphaEquation.HasValue ? (uint)BlendAlphaEquation.Value : Inherit16;
            values[11] = BlendAlphaSource.HasValue ? (uint)BlendAlphaSource.Value : Inherit32;
            values[12] = BlendAlphaDestination.HasValue ? (uint)BlendAlphaDestination.Value : Inherit32;

            if (ColorMask != null && ColorMask.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    values[13 + i] = ColorMask[i] ? 1u : 0u;
                }
            }

            values[4] = BitConverter.ToUInt32(BitConverter.GetBytes(DepthBias.HasValue && DepthBias.Value >= 0.0f ? DepthBias.Value : 0.0f), 0);
            values[6] = BitConverter.ToUInt32(BitConverter.GetBytes(AlphaReference.HasValue && AlphaReference.Value >= 0.0f ? AlphaReference.Value : 0.0f), 0);

            return values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        }

        private static byte GetByte(uint[] values, int index, int byteIndex)
        {
            return (byte)((values[index] >> (byteIndex * 8)) & 0xFF);
        }

        private static bool? ReadBool(byte value)
        {
            return value == InheritByte ? (bool?)null : value != 0;
        }

        private static int? ReadRaw(byte value)
        {
            return value == InheritByte ? (int?)null : value;
        }

        private void ReadV2(byte[] payload)
        {
            uint[] values = ToUInt32Array(payload, V2PayloadSize);

            DepthWrite = ReadBool(GetByte(values, 1, 1));
            Blend = ReadBool(GetByte(values, 2, 1));
            AlphaTest = ReadBool(GetByte(values, 2, 2));
            DepthTest = ReadBool(GetByte(values, 2, 3));
            DepthBiasEnable = ReadBool(GetByte(values, 3, 0));
            StencilTest = ReadBool(GetByte(values, 3, 1));
            Cull = ReadBool(GetByte(values, 3, 3));

            BlendRgbEquation = FromIndex(BlendEquationIndexes, GetByte(values, 6, 0));
            BlendRgbSource = FromIndex(BlendFactorIndexes, GetByte(values, 6, 1));
            BlendRgbDestination = FromIndex(BlendFactorIndexes, GetByte(values, 6, 2));
            BlendAlphaEquation = FromIndex(BlendEquationIndexes, GetByte(values, 6, 3));
            BlendAlphaSource = FromIndex(BlendFactorIndexes, GetByte(values, 7, 0));
            BlendAlphaDestination = FromIndex(BlendFactorIndexes, GetByte(values, 7, 1));

            AlphaFunction = FromIndex(CompareFunctionIndexes, GetByte(values, 10, 0));
            DepthFunction = FromIndex(CompareFunctionIndexes, GetByte(values, 11, 0));
            StencilFunction = FromIndex(CompareFunctionIndexes, GetByte(values, 12, 0));

            StencilFailOperation = ReadRaw(GetByte(values, 12, 1));
            StencilDepthFailOperation = ReadRaw(GetByte(values, 12, 2));
            StencilDepthPassOperation = ReadRaw(GetByte(values, 12, 3));

            byte colorMask = GetByte(values, 1, 0);
            ColorMask = colorMask == InheritByte
                ? null
                : new[] { (colorMask & 1) != 0, (colorMask & 2) != 0, (colorMask & 4) != 0, (colorMask & 8) != 0 };

            StencilWriteMask = ReadRaw(GetByte(values, 1, 2));

            ushort stencilReference = (ushort)(values[13] & 0xFFFF);
            StencilReference = stencilReference == InheritShort ? (int?)null : stencilReference;

            ushort stencilCompareMask = (ushort)(values[13] >> 16);
            StencilCompareMask = stencilCompareMask == InheritShort ? (int?)null : stencilCompareMask;

            float depthBias = BitConverter.ToSingle(BitConverter.GetBytes(values[8]), 0);
            DepthBias = depthBias >= 0.0f ? depthBias : (float?)null;

            float alphaReference = BitConverter.ToSingle(BitConverter.GetBytes(values[9]), 0);
            AlphaReference = alphaReference >= 0.0f ? alphaReference : (float?)null;
        }

        private byte[] WriteV2()
        {
            byte[] payload = Enumerable.Repeat(InheritByte, V2PayloadSize).ToArray();

            void SetByte(int index, int byteIndex, byte value) => payload[index * 4 + byteIndex] = value;
            void SetUInt32(int index, uint value) => Array.Copy(BitConverter.GetBytes(value), 0, payload, index * 4, 4);

            // The first int holds the payload size, the loader sets it itself and never reads it back
            SetUInt32(0, V2PayloadSize);

            void SetBool(int index, int byteIndex, bool? value)
            {
                if (value.HasValue)
                {
                    SetByte(index, byteIndex, value.Value ? (byte)1 : (byte)0);
                }
            }

            void SetRaw(int index, int byteIndex, int? value)
            {
                if (value.HasValue)
                {
                    SetByte(index, byteIndex, (byte)value.Value);
                }
            }

            SetBool(1, 1, DepthWrite);
            SetBool(2, 1, Blend);
            SetBool(2, 2, AlphaTest);
            SetBool(2, 3, DepthTest);
            SetBool(3, 0, DepthBiasEnable);
            SetBool(3, 1, StencilTest);
            SetBool(3, 3, Cull);

            SetByte(6, 0, ToIndex(BlendEquationIndexes, BlendRgbEquation));
            SetByte(6, 1, ToIndex(BlendFactorIndexes, BlendRgbSource));
            SetByte(6, 2, ToIndex(BlendFactorIndexes, BlendRgbDestination));
            SetByte(6, 3, ToIndex(BlendEquationIndexes, BlendAlphaEquation));
            SetByte(7, 0, ToIndex(BlendFactorIndexes, BlendAlphaSource));
            SetByte(7, 1, ToIndex(BlendFactorIndexes, BlendAlphaDestination));

            SetByte(10, 0, ToIndex(CompareFunctionIndexes, AlphaFunction));
            SetByte(11, 0, ToIndex(CompareFunctionIndexes, DepthFunction));
            SetByte(12, 0, ToIndex(CompareFunctionIndexes, StencilFunction));

            SetRaw(12, 1, StencilFailOperation);
            SetRaw(12, 2, StencilDepthFailOperation);
            SetRaw(12, 3, StencilDepthPassOperation);

            if (ColorMask != null && ColorMask.Length == 4)
            {
                byte colorMask = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (ColorMask[i])
                    {
                        colorMask |= (byte)(1 << i);
                    }
                }

                SetByte(1, 0, colorMask);
            }

            SetRaw(1, 2, StencilWriteMask);

            uint stencilReference = StencilReference.HasValue ? (uint)StencilReference.Value & 0xFFFF : InheritShort;
            uint stencilCompareMask = StencilCompareMask.HasValue ? (uint)StencilCompareMask.Value & 0xFFFF : InheritShort;
            SetUInt32(13, (stencilCompareMask << 16) | stencilReference);

            SetUInt32(8, BitConverter.ToUInt32(BitConverter.GetBytes(DepthBias.HasValue && DepthBias.Value >= 0.0f ? DepthBias.Value : InheritFloat), 0));
            SetUInt32(9, BitConverter.ToUInt32(BitConverter.GetBytes(AlphaReference.HasValue && AlphaReference.Value >= 0.0f ? AlphaReference.Value : InheritFloat), 0));

            return payload;
        }
    }
}
