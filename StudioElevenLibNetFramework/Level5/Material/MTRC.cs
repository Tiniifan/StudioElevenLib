using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.LZ10;
using static StudioElevenLib.Level5.Material.MTRCSupport;

namespace StudioElevenLib.Level5.Material
{
    /// <summary>
    /// The lighting material of one mesh: five colours, the fragment lighting flags and up to seven
    /// lookup tables. The header holds the offsets of the LUTC sub files, one per table, in the order
    /// RR RG RB D0 D1 SP FR.
    /// </summary>
    public class MTRC
    {
        public int FileVersion { get; set; }

        public float[] Ambient { get; set; }

        public float[] Emission { get; set; }

        public float[] Diffuse { get; set; }

        public float[] Specular0 { get; set; }

        public float[] Specular1 { get; set; }

        /// <summary>
        /// Layer configuration of the fragment lighting, five bits.
        /// </summary>
        public int Config { get; set; }

        public bool LutEnabledD0 { get; set; }

        public bool LutEnabledD1 { get; set; }

        public bool LutEnabledRefl { get; set; }

        public int ShadowSelector { get; set; }

        public bool InvertShadow { get; set; }

        public bool ShadowPrimary { get; set; }

        public bool ShadowSecondary { get; set; }

        public bool ShadowAlpha { get; set; }

        public bool BumpRenorm { get; set; }

        public int BumpSelector { get; set; }

        public int BumpMode { get; set; }

        public bool ClampHighlights { get; set; }

        public FresnelSelector Fresnel { get; set; }

        /// <summary>
        /// One entry per table, keyed by D0, D1, SP, FR, RR, RG and RB.
        /// </summary>
        public Dictionary<string, MaterialTable> Tables { get; set; }

        /// <summary>
        /// Ints the loader never reads out of the file, kept so a file comes back unchanged.
        /// </summary>
        public Dictionary<int, uint> Unused { get; set; }

        /// <summary>
        /// True when the file used the XMTR container instead of MTRC00.
        /// </summary>
        public bool Legacy { get; set; }

        public MTRC() : this(2)
        {
        }

        public MTRC(int fileVersion)
        {
            FileVersion = fileVersion;

            Ambient = new[] { 0.2f, 0.2f, 0.2f };
            Emission = new[] { 0.0f, 0.0f, 0.0f };
            Diffuse = new[] { 0.8f, 0.8f, 0.8f };
            Specular0 = new[] { 0.0f, 0.0f, 0.0f };
            Specular1 = new[] { 0.0f, 0.0f, 0.0f };

            Tables = new Dictionary<string, MaterialTable>();

            for (int i = 0; i < MTRCSupport.Tables.Length; i++)
            {
                Tables[MTRCSupport.Tables[i]] = new MaterialTable { InputSelect = (LutInput)Defaults[InputSelect + i] };
            }

            Unused = new Dictionary<int, uint>();

            foreach (int index in UnusedInts)
            {
                Unused[index] = 0;
            }
        }

        public MTRC(byte[] data) : this(2)
        {
            int bodyOffset;
            ushort[] slots = new ushort[7];

            if (data.Length >= LegacyHeaderSize && Encoding.ASCII.GetString(data, 0, 4) == "XMTR")
            {
                // Legacy container, a few IEGO models still ship it
                Legacy = true;
                bodyOffset = BitConverter.ToUInt16(data, 4);

                if (bodyOffset == 0)
                {
                    bodyOffset = LegacyHeaderSize;
                }

                for (int i = 0; i < 7; i++)
                {
                    slots[i] = BitConverter.ToUInt16(data, 6 + i * 2);
                }
            }
            else
            {
                if (data.Length < HeaderSize || Encoding.ASCII.GetString(data, 0, 4) != "MTRC")
                {
                    throw new InvalidDataException("Unknown MTR magic");
                }

                bodyOffset = BitConverter.ToUInt16(data, 8);

                if (bodyOffset == 0)
                {
                    bodyOffset = HeaderSize;
                }

                for (int i = 0; i < 7; i++)
                {
                    slots[i] = BitConverter.ToUInt16(data, 10 + i * 2);
                }
            }

            if (bodyOffset + 4 > data.Length)
            {
                throw new InvalidDataException("The MTR file is too small");
            }

            // Some decoders produce a few extra bytes, the block header holds the real size
            int declared = (int)(BitConverter.ToUInt32(data, bodyOffset) >> 3);
            byte[] payload = Compressor.Decompress(data.Skip(bodyOffset).ToArray());
            int intCount = Math.Min(Math.Min(payload.Length, declared) / 4, V2IntCount);

            uint[] values = (uint[])Defaults.Clone();

            for (int i = 0; i < intCount; i++)
            {
                values[i] = BitConverter.ToUInt32(payload, i * 4);
            }

            FileVersion = intCount <= V1IntCount ? 1 : 2;

            ReadBody(values);

            for (int i = 0; i < 7; i++)
            {
                if (slots[i] == 0 || slots[i] >= data.Length)
                {
                    continue;
                }

                Tables[HeaderTables[i]].Lut = new LUTC(data.Skip(slots[i]).ToArray(), Legacy);
            }
        }

        private void ReadBody(uint[] values)
        {
            for (int i = 0; i < MTRCSupport.Tables.Length; i++)
            {
                MaterialTable table = Tables[MTRCSupport.Tables[i]];
                table.AbsInput = (values[AbsInput + i] & 1) != 0;
                table.InputSelect = (LutInput)(values[InputSelect + i] & 0xFF);
                table.Scale = (int)(values[InputScale + i] & 0xFF);
            }

            LutEnabledD0 = (values[LutEnabled] & 1) != 0;
            LutEnabledD1 = (values[LutEnabled + 1] & 1) != 0;
            LutEnabledRefl = (values[LutEnabled + 2] & 1) != 0;
            Config = (int)(values[ConfigIndex] & 0x1F);

            Ambient = ReadColor(values, 0);
            Emission = ReadColor(values, 1);
            Diffuse = ReadColor(values, 2);
            Specular0 = ReadColor(values, 3);
            Specular1 = ReadColor(values, 4);

            ShadowSelector = (int)(values[47] & 0xFF);
            InvertShadow = (values[48] & 1) != 0;
            ShadowPrimary = (values[49] & 1) != 0;
            ShadowSecondary = (values[50] & 1) != 0;
            ShadowAlpha = (values[51] & 1) != 0;
            BumpRenorm = (values[52] & 1) != 0;
            BumpSelector = (int)(values[53] & 0xFF);
            BumpMode = (int)(values[54] & 0xFF);
            ClampHighlights = (values[55] & 1) != 0;
            Fresnel = (FresnelSelector)(values[56] & 0xFF);

            foreach (int index in UnusedInts)
            {
                Unused[index] = values[index];
            }
        }

        private static float[] ReadColor(uint[] values, int index)
        {
            return Enumerable.Range(0, 3)
                .Select(component => BitConverter.ToSingle(BitConverter.GetBytes(values[ColorIndex + index * 3 + component]), 0))
                .ToArray();
        }

        public byte[] WriteBody(int intCount)
        {
            uint[] values = Defaults.Take(intCount).ToArray();

            for (int i = 0; i < MTRCSupport.Tables.Length; i++)
            {
                MaterialTable table = Tables[MTRCSupport.Tables[i]];
                values[AbsInput + i] = table.AbsInput ? 1u : 0u;
                values[InputSelect + i] = (uint)table.InputSelect;
                values[InputScale + i] = (uint)table.Scale;
            }

            values[LutEnabled] = LutEnabledD0 ? 1u : 0u;
            values[LutEnabled + 1] = LutEnabledD1 ? 1u : 0u;
            values[LutEnabled + 2] = LutEnabledRefl ? 1u : 0u;
            values[ConfigIndex] = (uint)(Config & 0x1F);

            WriteColor(values, 0, Ambient);
            WriteColor(values, 1, Emission);
            WriteColor(values, 2, Diffuse);
            WriteColor(values, 3, Specular0);
            WriteColor(values, 4, Specular1);

            values[47] = (uint)(ShadowSelector & 0xFF);
            values[48] = InvertShadow ? 1u : 0u;
            values[49] = ShadowPrimary ? 1u : 0u;
            values[50] = ShadowSecondary ? 1u : 0u;
            values[51] = ShadowAlpha ? 1u : 0u;
            values[52] = BumpRenorm ? 1u : 0u;
            values[53] = (uint)(BumpSelector & 0xFF);
            values[54] = (uint)(BumpMode & 0xFF);
            values[55] = ClampHighlights ? 1u : 0u;
            values[56] = (uint)((int)Fresnel & 0xFF);

            foreach (int index in UnusedInts)
            {
                if (index < intCount)
                {
                    values[index] = Unused[index];
                }
            }

            return values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        }

        private static void WriteColor(uint[] values, int index, float[] color)
        {
            for (int component = 0; component < 3; component++)
            {
                values[ColorIndex + index * 3 + component] = BitConverter.ToUInt32(BitConverter.GetBytes(color[component]), 0);
            }
        }

        public byte[] Save()
        {
            return Save(FileVersion);
        }

        /// <summary>
        /// fileVersion is the one of the game engine, V1 writes a 57 int body and V2 a 58 int one.
        /// </summary>
        public byte[] Save(int fileVersion)
        {
            if (fileVersion != 1 && fileVersion != 2)
            {
                throw new NotSupportedException("Unsupported MTR file version: " + fileVersion);
            }

            byte[] body = new LZ10().Compress(WriteBody(fileVersion == 1 ? V1IntCount : V2IntCount));

            ushort[] offsets = new ushort[7];
            List<byte[]> blocks = new List<byte[]>();
            int position = HeaderSize + body.Length;

            for (int i = 0; i < 7; i++)
            {
                LUTC lut = Tables[HeaderTables[i]].Lut;

                if (lut == null)
                {
                    continue;
                }

                // Tables that hold the same values share one sub file, like the shipped IEGO materials do
                int shared = -1;

                for (int other = 0; other < i; other++)
                {
                    if (offsets[other] != 0 && lut.SameValues(Tables[HeaderTables[other]].Lut))
                    {
                        shared = offsets[other];
                        break;
                    }
                }

                if (shared >= 0)
                {
                    offsets[i] = (ushort)shared;
                    continue;
                }

                byte[] block = lut.Save();
                offsets[i] = (ushort)position;
                position += block.Length;
                blocks.Add(block);
            }

            using (MemoryStream stream = new MemoryStream())
            {
                byte[] magic = Encoding.ASCII.GetBytes("MTRC00");
                stream.Write(magic, 0, magic.Length);
                stream.Write(new byte[] { 0, 0 }, 0, 2);
                stream.Write(BitConverter.GetBytes((ushort)HeaderSize), 0, 2);

                foreach (ushort offset in offsets)
                {
                    stream.Write(BitConverter.GetBytes(offset), 0, 2);
                }

                stream.Write(body, 0, body.Length);

                foreach (byte[] block in blocks)
                {
                    stream.Write(block, 0, block.Length);
                }

                return stream.ToArray();
            }
        }
    }
}
