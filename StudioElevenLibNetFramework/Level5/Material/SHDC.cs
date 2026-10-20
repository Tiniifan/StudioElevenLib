using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StudioElevenLib.Level5.Material
{
    /// <summary>
    /// A PICA200 shader binary (DVLB) of a SHDnnn.shdc file: the code shared by its programs (DVLP)
    /// and one DVLE per program, with its constants, labels, outputs and the uniform table the game
    /// binds by name. The data given and returned is not compressed.
    /// </summary>
    public class SHDC
    {
        /// <summary>
        /// 0x0A02 in the V1 game, 0x0C02 in the V2 games.
        /// </summary>
        public uint Version { get; set; }

        public List<uint> Code { get; set; }

        /// <summary>
        /// Two ints per descriptor: the masks and swizzles, then the flags of the fields the instructions use.
        /// </summary>
        public List<ulong> OperandDescriptors { get; set; }

        public List<string> FileNames { get; set; }

        public List<SHDCProgram> Programs { get; set; }

        public static readonly Dictionary<int, uint> Versions = new Dictionary<int, uint>
        {
            { 1, 0x0A02 },
            { 2, 0x0C02 },
        };

        public SHDC()
        {
            Version = Versions[2];
            Code = new List<uint>();
            OperandDescriptors = new List<ulong>();
            FileNames = new List<string>();
            Programs = new List<SHDCProgram>();
        }

        public SHDC(byte[] data) : this()
        {
            if (data.Length < 8 || Encoding.ASCII.GetString(data, 0, 4) != "DVLB")
            {
                throw new InvalidDataException("Unknown shader binary magic");
            }

            int count = BitConverter.ToInt32(data, 4);
            int[] programOffsets = new int[count];

            for (int i = 0; i < count; i++)
            {
                programOffsets[i] = BitConverter.ToInt32(data, 8 + i * 4);
            }

            int dvlp = 8 + count * 4;

            if (Encoding.ASCII.GetString(data, dvlp, 4) != "DVLP")
            {
                throw new InvalidDataException("Unknown shader code magic");
            }

            Version = BitConverter.ToUInt32(data, dvlp + 4);
            int codeOffset = BitConverter.ToInt32(data, dvlp + 8);
            int codeSize = BitConverter.ToInt32(data, dvlp + 12);
            int descriptorOffset = BitConverter.ToInt32(data, dvlp + 16);
            int descriptorCount = BitConverter.ToInt32(data, dvlp + 20);
            int fileNameOffset = BitConverter.ToInt32(data, dvlp + 32);
            int fileNameSize = BitConverter.ToInt32(data, dvlp + 36);

            for (int i = 0; i < codeSize; i++)
            {
                Code.Add(BitConverter.ToUInt32(data, dvlp + codeOffset + i * 4));
            }

            for (int i = 0; i < descriptorCount; i++)
            {
                OperandDescriptors.Add(BitConverter.ToUInt64(data, dvlp + descriptorOffset + i * 8));
            }

            string names = Encoding.ASCII.GetString(data, dvlp + fileNameOffset, fileNameSize);
            FileNames = names.Split('\0').Where(name => name.Length > 0).ToList();

            foreach (int offset in programOffsets)
            {
                Programs.Add(new SHDCProgram(data, offset));
            }
        }

        public byte[] Save()
        {
            byte[] fileNames = FileNames.SelectMany(name => Encoding.ASCII.GetBytes(name).Concat(new byte[] { 0 })).ToArray();

            using (MemoryStream dvlp = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(dvlp))
            {
                int codeOffset = 0x28;
                int descriptorOffset = codeOffset + Code.Count * 4;
                int fileNameOffset = descriptorOffset + OperandDescriptors.Count * 8;

                writer.Write(Encoding.ASCII.GetBytes("DVLP"));
                writer.Write(Version);
                writer.Write(codeOffset);
                writer.Write(Code.Count);
                writer.Write(descriptorOffset);
                writer.Write(OperandDescriptors.Count);
                writer.Write(fileNameOffset);
                writer.Write(0);
                writer.Write(fileNameOffset);
                writer.Write(fileNames.Length);

                foreach (uint word in Code)
                {
                    writer.Write(word);
                }

                foreach (ulong descriptor in OperandDescriptors)
                {
                    writer.Write(descriptor);
                }

                writer.Write(fileNames);

                while (dvlp.Length % 4 != 0)
                {
                    writer.Write((byte)0);
                }

                List<byte[]> programs = Programs.Select(program => program.Save()).ToList();

                using (MemoryStream output = new MemoryStream())
                using (BinaryWriter outputWriter = new BinaryWriter(output))
                {
                    outputWriter.Write(Encoding.ASCII.GetBytes("DVLB"));
                    outputWriter.Write(programs.Count);

                    int offset = 8 + programs.Count * 4 + (int)dvlp.Length;

                    foreach (byte[] program in programs)
                    {
                        outputWriter.Write(offset);
                        offset += program.Length;
                    }

                    outputWriter.Write(dvlp.ToArray());

                    foreach (byte[] program in programs)
                    {
                        outputWriter.Write(program);
                    }

                    return output.ToArray();
                }
            }
        }

        public static float Float24ToFloat(uint value)
        {
            if ((value & 0x7FFFFF) == 0)
            {
                return 0.0f;
            }

            uint sign = (value >> 23) & 1;
            uint exponent = (value >> 16) & 0x7F;
            uint mantissa = value & 0xFFFF;
            uint bits = (sign << 31) | ((exponent - 63 + 127) << 23) | (mantissa << 7);

            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }

        public static uint FloatToFloat24(float value)
        {
            uint bits = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            uint sign = bits >> 31;
            int exponent = (int)((bits >> 23) & 0xFF) - 127 + 63;
            uint mantissa = (bits >> 7) & 0xFFFF;

            if (value == 0.0f || exponent <= 0)
            {
                return sign << 23;
            }

            if (exponent >= 0x7F)
            {
                exponent = 0x7F;
                mantissa = 0;
            }

            return (sign << 23) | ((uint)exponent << 16) | mantissa;
        }

        /// <summary>
        /// The components of an output are bit 0 = x to bit 3 = w, the reverse of an operand descriptor.
        /// </summary>
        public static string ReadOutputMask(int bits)
        {
            string mask = "";

            for (int i = 0; i < 4; i++)
            {
                if ((bits & (1 << i)) != 0)
                {
                    mask += "xyzw"[i];
                }
            }

            return mask;
        }

        public static int WriteOutputMask(string mask)
        {
            int bits = 0;

            for (int i = 0; i < 4; i++)
            {
                if (mask.Contains("xyzw"[i]))
                {
                    bits |= 1 << i;
                }
            }

            return bits;
        }
    }

    public class SHDCProgram
    {
        public ushort Version { get; set; }

        /// <summary>
        /// 0 vertex program, 1 geometry program.
        /// </summary>
        public byte Type { get; set; }

        public byte MergeOutputs { get; set; }

        public int Main { get; set; }

        public int EndMain { get; set; }

        public ushort InputMask { get; set; }

        public ushort OutputMask { get; set; }

        public byte[] Geometry { get; set; }

        public List<SHDCConstant> Constants { get; set; }

        public List<SHDCLabel> Labels { get; set; }

        public List<SHDCOutput> Outputs { get; set; }

        /// <summary>
        /// The names the game binds: the attributes (atr_pos.xyzw...) at their input register, the uniforms
        /// from 16 for the float registers.
        /// </summary>
        public List<SHDCUniform> Uniforms { get; set; }

        public SHDCProgram()
        {
            Geometry = new byte[4];
            Constants = new List<SHDCConstant>();
            Labels = new List<SHDCLabel>();
            Outputs = new List<SHDCOutput>();
            Uniforms = new List<SHDCUniform>();
        }

        public SHDCProgram(byte[] data, int offset) : this()
        {
            if (Encoding.ASCII.GetString(data, offset, 4) != "DVLE")
            {
                throw new InvalidDataException("Unknown shader program magic");
            }

            Version = BitConverter.ToUInt16(data, offset + 4);
            Type = data[offset + 6];
            MergeOutputs = data[offset + 7];
            Main = BitConverter.ToInt32(data, offset + 8);
            EndMain = BitConverter.ToInt32(data, offset + 12);
            InputMask = BitConverter.ToUInt16(data, offset + 16);
            OutputMask = BitConverter.ToUInt16(data, offset + 18);
            Geometry = data.Skip(offset + 20).Take(4).ToArray();

            int constantOffset = BitConverter.ToInt32(data, offset + 24);
            int constantCount = BitConverter.ToInt32(data, offset + 28);
            int labelOffset = BitConverter.ToInt32(data, offset + 32);
            int labelCount = BitConverter.ToInt32(data, offset + 36);
            int outputOffset = BitConverter.ToInt32(data, offset + 40);
            int outputCount = BitConverter.ToInt32(data, offset + 44);
            int uniformOffset = BitConverter.ToInt32(data, offset + 48);
            int uniformCount = BitConverter.ToInt32(data, offset + 52);
            int symbols = offset + BitConverter.ToInt32(data, offset + 56);

            for (int i = 0; i < constantCount; i++)
            {
                int position = offset + constantOffset + i * 20;
                SHDCConstant constant = new SHDCConstant
                {
                    Type = BitConverter.ToUInt16(data, position),
                    Register = BitConverter.ToUInt16(data, position + 2),
                    Raw = new uint[4],
                };

                for (int k = 0; k < 4; k++)
                {
                    constant.Raw[k] = BitConverter.ToUInt32(data, position + 4 + k * 4);
                }

                Constants.Add(constant);
            }

            for (int i = 0; i < labelCount; i++)
            {
                int position = offset + labelOffset + i * 16;
                Labels.Add(new SHDCLabel
                {
                    Id = data[position],
                    Unk = data.Skip(position + 1).Take(3).ToArray(),
                    Offset = BitConverter.ToInt32(data, position + 4),
                    Size = BitConverter.ToUInt32(data, position + 8),
                    Name = ReadSymbol(data, symbols + BitConverter.ToInt32(data, position + 12)),
                });
            }

            for (int i = 0; i < outputCount; i++)
            {
                int position = offset + outputOffset + i * 8;
                Outputs.Add(new SHDCOutput
                {
                    Type = (SHDCOutputType)BitConverter.ToUInt16(data, position),
                    Register = BitConverter.ToUInt16(data, position + 2),
                    Mask = SHDC.ReadOutputMask(BitConverter.ToUInt16(data, position + 4)),
                });
            }

            for (int i = 0; i < uniformCount; i++)
            {
                int position = offset + uniformOffset + i * 8;
                Uniforms.Add(new SHDCUniform
                {
                    Name = ReadSymbol(data, symbols + BitConverter.ToInt32(data, position)),
                    Start = BitConverter.ToUInt16(data, position + 4),
                    End = BitConverter.ToUInt16(data, position + 6),
                });
            }
        }

        private static string ReadSymbol(byte[] data, int offset)
        {
            int end = Array.IndexOf(data, (byte)0, offset);
            return Encoding.ASCII.GetString(data, offset, end - offset);
        }

        public byte[] Save()
        {
            List<byte> symbols = new List<byte>();
            Dictionary<string, int> symbolOffsets = new Dictionary<string, int>();

            foreach (string name in Labels.Select(label => label.Name).Concat(Uniforms.Select(uniform => uniform.Name)))
            {
                if (!symbolOffsets.ContainsKey(name))
                {
                    symbolOffsets[name] = symbols.Count;
                    symbols.AddRange(Encoding.ASCII.GetBytes(name));
                    symbols.Add(0);
                }
            }

            int constantOffset = 0x40;
            int labelOffset = constantOffset + Constants.Count * 20;
            int outputOffset = labelOffset + Labels.Count * 16;
            int uniformOffset = outputOffset + Outputs.Count * 8;
            int symbolOffset = uniformOffset + Uniforms.Count * 8;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("DVLE"));
                writer.Write(Version);
                writer.Write(Type);
                writer.Write(MergeOutputs);
                writer.Write(Main);
                writer.Write(EndMain);
                writer.Write(InputMask);
                writer.Write(OutputMask);
                writer.Write(Geometry);
                writer.Write(constantOffset);
                writer.Write(Constants.Count);
                writer.Write(labelOffset);
                writer.Write(Labels.Count);
                writer.Write(outputOffset);
                writer.Write(Outputs.Count);
                writer.Write(uniformOffset);
                writer.Write(Uniforms.Count);
                writer.Write(symbolOffset);
                writer.Write(symbols.Count);

                foreach (SHDCConstant constant in Constants)
                {
                    writer.Write(constant.Type);
                    writer.Write(constant.Register);

                    foreach (uint value in constant.Raw)
                    {
                        writer.Write(value);
                    }
                }

                foreach (SHDCLabel label in Labels)
                {
                    writer.Write(label.Id);
                    writer.Write(label.Unk ?? new byte[3]);
                    writer.Write(label.Offset);
                    writer.Write(label.Size);
                    writer.Write(symbolOffsets[label.Name]);
                }

                foreach (SHDCOutput output in Outputs)
                {
                    writer.Write((ushort)output.Type);
                    writer.Write(output.Register);
                    writer.Write((ushort)SHDC.WriteOutputMask(output.Mask));
                    writer.Write((ushort)0);
                }

                foreach (SHDCUniform uniform in Uniforms)
                {
                    writer.Write(symbolOffsets[uniform.Name]);
                    writer.Write(uniform.Start);
                    writer.Write(uniform.End);
                }

                writer.Write(symbols.ToArray());

                while (stream.Length % 4 != 0)
                {
                    writer.Write((byte)0);
                }

                return stream.ToArray();
            }
        }
    }

    public class SHDCConstant
    {
        /// <summary>
        /// 0 bool, 1 int, 2 float (four float24).
        /// </summary>
        public ushort Type { get; set; }

        public ushort Register { get; set; }

        public uint[] Raw { get; set; }

        public float[] GetFloats()
        {
            return Raw.Select(SHDC.Float24ToFloat).ToArray();
        }

        public void SetFloats(float[] values)
        {
            Raw = values.Select(SHDC.FloatToFloat24).ToArray();
        }
    }

    public class SHDCLabel
    {
        public byte Id { get; set; }

        public byte[] Unk { get; set; }

        public int Offset { get; set; }

        public uint Size { get; set; }

        public string Name { get; set; }
    }

    public class SHDCOutput
    {
        public SHDCOutputType Type { get; set; }

        public ushort Register { get; set; }

        public string Mask { get; set; }
    }

    public class SHDCUniform
    {
        public string Name { get; set; }

        public ushort Start { get; set; }

        public ushort End { get; set; }
    }

    public enum SHDCOutputType : ushort
    {
        Position = 0,
        NormalQuaternion = 1,
        Color = 2,
        Texcoord0 = 3,
        Texcoord0W = 4,
        Texcoord1 = 5,
        Texcoord2 = 6,
        View = 8,
        Dummy = 9,
    }
}
