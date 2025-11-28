using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.NoCompression;
using StudioElevenLib.Level5.Resource.Logic;

namespace StudioElevenLib.Level5.Resource.RES
{
    public class RES : IResource
    {
        public string Name => "RES";

        public List<string> StringTable { get; set; }

        public Dictionary<RESType, List<byte[]>> Items { get; set; }

        public RES(Stream stream)
        {
            StringTable = new List<string>();
            Items = new Dictionary<RESType, List<byte[]>>();

            using (BinaryDataReader reader = new BinaryDataReader(Compressor.Decompress(stream))) 
            {
                RESSupport.Header header = reader.ReadStruct<RESSupport.Header>();

                reader.Seek(header.StringOffset);
                using (BinaryDataReader textReader = new BinaryDataReader(reader.GetSection((int)(reader.Length - reader.Position))))
                {
                    while (textReader.Position < textReader.Length)
                    {
                        string name = textReader.ReadString(Encoding.GetEncoding(932));

                        if (name != "" && name != " ")
                        {
                            StringTable.Add(name);
                        }
                    }
                }

                ReadSectionTable(reader, header.MaterialTableOffset, header.MaterialTableCount);
                ReadSectionTable(reader, header.NodeOffset, header.NodeCount);
            }
        }

        public RES(byte[] data)
        {
            StringTable = new List<string>();
            Items = new Dictionary<RESType, List<byte[]>>();

            using (BinaryDataReader reader = new BinaryDataReader(Compressor.Decompress(data)))
            {
                RESSupport.Header header = reader.ReadStruct<RESSupport.Header>();

                reader.Seek(header.StringOffset);
                using (BinaryDataReader textReader = new BinaryDataReader(reader.GetSection((int)(reader.Length - reader.Position))))
                {
                    while (textReader.Position < textReader.Length)
                    {
                        string name = textReader.ReadString(Encoding.GetEncoding(932));

                        if (name != "" && name != " ")
                        {
                            StringTable.Add(name);
                            
                        }
                    }
                }

                ReadSectionTable(reader, header.MaterialTableOffset, header.MaterialTableCount);
                ReadSectionTable(reader, header.NodeOffset, header.NodeCount);
            }
        }

        public RES(List<string> stringTable, Dictionary<RESType, List<byte[]>> items)
        {
            StringTable = stringTable;
            Items = items;
        }

        public void Save(string filepath)
        {

        }

        public byte[] Save(byte[] magic)
        {
            // Split Items into 2 dictionaries
            Dictionary<RESType, List<byte[]>> materials = Items.Where(item => RESSupport.Materials.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value);
            Dictionary<RESType, List<byte[]>> nodes = Items.Where(item => RESSupport.Nodes.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value);

            using (MemoryStream fileStream = new MemoryStream())
            {
                BinaryDataWriter writerComp = new BinaryDataWriter(fileStream);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                    using (MemoryStream dataStream = new MemoryStream())
                    {
                        BinaryDataWriter writerData = new BinaryDataWriter(dataStream);

                        RESSupport.Header header = new RESSupport.Header();
                        header.Magic = BitConverter.ToInt64(magic, 0);
                        header.Unk1 = 1;
                        header.MaterialTableCount = (short)materials.Count;
                        header.NodeCount = (short)nodes.Count;

                        int headerPos = 20;
                        int dataPos = Items.Count * 8;

                        // Material - Header table
                        if (materials.Count > 0)
                        {
                            writerDecomp.Seek(headerPos);
                            header._materialTableOffset = (short)(headerPos >> 2);

                            for (int i = 0; i < materials.Count; i++)
                            {
                                KeyValuePair<RESType, List<byte[]>> resType = materials.ElementAt(i);

                                RESSupport.HeaderTable materialHeaderTable = new RESSupport.HeaderTable()
                                {
                                    _dataOffset = (short)(dataPos >> 2),
                                    Count = (short)resType.Value.Count,
                                    Type = (short)resType.Key,
                                    Length = (short)resType.Value[0].Length
                                };

                                headerPos += 8;
                                dataPos += resType.Value.Sum(byteArray => byteArray.Length);

                                writerDecomp.WriteStruct(materialHeaderTable);
                                writerData.Write(resType.Value.SelectMany(bytes => bytes).ToArray());
                            }
                        }

                        // Node - Header table
                        if (nodes.Count > 0)
                        {
                            writerDecomp.Seek(headerPos);
                            header._nodeOffset = (short)(headerPos >> 2);

                            for (int i = 0; i < nodes.Count; i++)
                            {
                                KeyValuePair<RESType, List<byte[]>> resType = nodes.ElementAt(i);

                                RESSupport.HeaderTable nodeHeaderTable = new RESSupport.HeaderTable()
                                {
                                    _dataOffset = (short)(dataPos >> 2),
                                    Count = (short)resType.Value.Count,
                                    Type = (short)resType.Key,
                                    Length = (short)resType.Value[0].Length
                                };

                                headerPos += 8;
                                dataPos += resType.Value.Sum(byteArray => byteArray.Length);

                                writerDecomp.WriteStruct(nodeHeaderTable);
                                writerData.Write(resType.Value.SelectMany(bytes => bytes).ToArray());
                            }
                        }

                        writerDecomp.Write(dataStream.ToArray());

                        // String table
                        header._stringOffset = (short)(writerDecomp.Position>>2);
                        for (int i = 0; i < StringTable.Count; i++)
                        {
                            writerDecomp.Write(Encoding.GetEncoding(932).GetBytes(StringTable[i]));
                            writerDecomp.Write((byte)0);
                        }

                        writerDecomp.WriteAlignment(4);

                        writerDecomp.Seek(0);
                        writerDecomp.WriteStruct(header);
                    }

                    // Compress
                   writerComp.Write(memoryStream.ToArray());
                }

                return fileStream.ToArray();
            }
        }

        private void ReadSectionTable(BinaryDataReader reader, int tableOffset, int tableCount)
        {
            for (int i = 0; i < tableCount; i++)
            {
                reader.Seek(tableOffset + i * 8);

                RESSupport.HeaderTable headerTable = reader.ReadStruct<RESSupport.HeaderTable>();

                if (!Items.ContainsKey((RESType)headerTable.Type))
                {
                    Items.Add((RESType)headerTable.Type, new List<byte[]>());
                }

                for (int j = 0; j < headerTable.Count; j++)
                {
                    reader.Seek((uint)(headerTable.DataOffset + j * headerTable.Length));
                    Items[(RESType)headerTable.Type].Add(reader.GetSection(headerTable.Length));
                }
            }
        }
    }
}
