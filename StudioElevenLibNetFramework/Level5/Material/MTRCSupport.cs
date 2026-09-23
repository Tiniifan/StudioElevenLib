using System;
using System.IO;
using System.Linq;
using System.Text;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.LZ10;

namespace StudioElevenLib.Level5.Material
{
    public class MTRCSupport
    {
        public const int HeaderSize = 0x18;

        public const int LegacyHeaderSize = 0x14;

        // V1 keeps 57 ints, V2 reads one more, both loaders fill what the body doesn't cover with Defaults
        public const int V1IntCount = 57;

        public const int V2IntCount = 58;

        public const int LutEntryCount = 256;

        public const int LutHeaderSize = 0x10;

        public const int LegacyLutHeaderSize = 0xC;

        /// <summary>
        /// The order registers 0x1D0 to 0x1D2 pack their seven 4 bit fields in, which is also the order of the body.
        /// </summary>
        public static readonly string[] Tables = { "D0", "D1", "SP", "FR", "RR", "RG", "RB" };

        /// <summary>
        /// The seven header offsets are not in that order, the loader rotates them by three.
        /// </summary>
        public static readonly string[] HeaderTables = { "RR", "RG", "RB", "D0", "D1", "SP", "FR" };

        /// <summary>
        /// lutScale* of the fragment shaders, the two holes are values the hardware doesn't define.
        /// </summary>
        public static readonly float?[] LutScales = { 1.0f, 2.0f, 4.0f, 8.0f, null, null, 0.25f, 0.5f };

        public static readonly string[] Colors = { "ambient", "emission", "diffuse", "specular0", "specular1" };

        // Body index of the first int of each field group
        public const int AbsInput = 0;

        public const int LutEnabled = 7;

        public const int InputSelect = 10;

        public const int InputScale = 17;

        public const int LutPointers = 24;

        public const int ConfigIndex = 31;

        public const int ColorIndex = 32;

        public const int FlagsIndex = 47;

        /// <summary>
        /// Ints the loader never reads out of the file: 24 to 30 are the stack slots it overwrites with the
        /// LUT pointers, 57 is the V2 tail and holds uninitialised bytes in every shipped file.
        /// </summary>
        public static readonly int[] UnusedInts = { 24, 25, 26, 27, 28, 29, 30, 57 };

        /// <summary>
        /// What the loaders prefill their buffer with, a shorter body keeps these.
        /// </summary>
        public static readonly uint[] Defaults = BuildDefaults();

        private static uint[] BuildDefaults()
        {
            uint[] values = new uint[V2IntCount];

            // SP reads the spot term, every other table the light direction
            values[InputSelect + 0] = 3;
            values[InputSelect + 1] = 3;
            values[InputSelect + 2] = 4;
            values[InputSelect + 3] = 3;
            values[InputSelect + 4] = 3;
            values[InputSelect + 5] = 3;
            values[InputSelect + 6] = 3;

            for (int i = 0; i < 3; i++)
            {
                values[ColorIndex + i] = 0x3E4CCCCD;
                values[ColorIndex + 6 + i] = 0x3F4CCCCD;
            }

            return values;
        }

        public enum LutInput
        {
            NH = 0,
            VH = 1,
            NV = 2,
            LN = 3,
            SP = 4,
            CP = 5
        }

        public enum FresnelSelector
        {
            None = 0,
            Primary = 1,
            Secondary = 2,
            Both = 3
        }

        /// <summary>
        /// 256 values and the 256 differences to the next value, the flag is a byte the engine keeps next to the table.
        /// </summary>
        public class LUTC
        {
            public float[] Values { get; set; }

            public float[] Deltas { get; set; }

            public int Flag { get; set; }

            public LUTC()
            {
                Values = new float[LutEntryCount];
                Deltas = new float[LutEntryCount];
            }

            public LUTC(byte[] data) : this(data, false)
            {
            }

            /// <summary>
            /// The XLUT sub file of the legacy container puts the payload offset first and its flag is an int.
            /// </summary>
            public LUTC(byte[] data, bool legacy) : this()
            {
                int payloadOffset;

                if (legacy)
                {
                    if (data.Length < LegacyLutHeaderSize || Encoding.ASCII.GetString(data, 0, 4) != "XLUT")
                    {
                        throw new InvalidDataException("Unknown LUT magic");
                    }

                    payloadOffset = BitConverter.ToUInt16(data, 4);
                    Flag = (int)(BitConverter.ToUInt32(data, 8) & 0xFF);

                    if (payloadOffset == 0)
                    {
                        payloadOffset = LegacyLutHeaderSize;
                    }
                }
                else
                {
                    if (data.Length < LutHeaderSize || Encoding.ASCII.GetString(data, 0, 6) != "LUTC00")
                    {
                        throw new InvalidDataException("Unknown LUT magic");
                    }

                    Flag = BitConverter.ToUInt16(data, 8);
                    payloadOffset = BitConverter.ToUInt16(data, 10);
                }

                byte[] payload = Compressor.Decompress(data.Skip(payloadOffset).ToArray());

                if (payload.Length < LutEntryCount * 8)
                {
                    throw new InvalidDataException("Unexpected LUT payload size: " + payload.Length);
                }

                for (int i = 0; i < LutEntryCount; i++)
                {
                    Values[i] = BitConverter.ToSingle(payload, i * 4);
                    Deltas[i] = BitConverter.ToSingle(payload, LutEntryCount * 4 + i * 4);
                }
            }

            public byte[] Save()
            {
                byte[] payload = new byte[LutEntryCount * 8];

                for (int i = 0; i < LutEntryCount; i++)
                {
                    Array.Copy(BitConverter.GetBytes(Values[i]), 0, payload, i * 4, 4);
                    Array.Copy(BitConverter.GetBytes(Deltas[i]), 0, payload, LutEntryCount * 4 + i * 4, 4);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    byte[] magic = Encoding.ASCII.GetBytes("LUTC00");
                    stream.Write(magic, 0, magic.Length);
                    stream.Write(new byte[] { 0, 0 }, 0, 2);
                    stream.Write(BitConverter.GetBytes((ushort)Flag), 0, 2);
                    stream.Write(BitConverter.GetBytes((ushort)LutHeaderSize), 0, 2);
                    stream.Write(BitConverter.GetBytes(0), 0, 4);

                    byte[] block = new LZ10().Compress(payload);
                    stream.Write(block, 0, block.Length);

                    return stream.ToArray();
                }
            }

            /// <summary>
            /// The 256 words of the GPU table: a 12 bit value and a 12 bit difference (sign and magnitude).
            /// </summary>
            public uint[] ToTable()
            {
                uint[] table = new uint[LutEntryCount];

                for (int i = 0; i < LutEntryCount; i++)
                {
                    table[i] = QuantizeValue(Values[i]) | (QuantizeDelta(Deltas[i]) << 12);
                }

                return table;
            }

            public static uint QuantizeValue(float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                {
                    return 0;
                }

                float scaled = value * 4096.0f;

                return scaled >= 4096.0f ? 4095u : (uint)scaled;
            }

            public static uint QuantizeDelta(float delta)
            {
                if (float.IsNaN(delta) || float.IsInfinity(delta) || delta == 0.0f)
                {
                    return 0;
                }

                float scaled = delta * 2048.0f;
                uint sign = scaled >= 0.0f ? 0u : 2048u;
                float magnitude = Math.Abs(scaled);

                return sign | (magnitude >= 2048.0f ? 2047u : (uint)magnitude);
            }

            public bool SameValues(LUTC other)
            {
                return other != null && Flag == other.Flag
                    && Values.SequenceEqual(other.Values) && Deltas.SequenceEqual(other.Deltas);
            }
        }

        /// <summary>
        /// Configuration of one lighting lookup table plus the table itself when the file carries one.
        /// </summary>
        public class MaterialTable
        {
            public bool AbsInput { get; set; }

            public LutInput InputSelect { get; set; }

            public int Scale { get; set; }

            public LUTC Lut { get; set; }

            public MaterialTable()
            {
                InputSelect = LutInput.LN;
            }

            public float? ScaleValue
            {
                get { return Scale < LutScales.Length ? LutScales[Scale] : null; }
            }
        }
    }
}
