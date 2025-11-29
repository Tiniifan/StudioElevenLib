using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Compression.LZ10;
using StudioElevenLib.Level5.Resource.Types.Scene3D;

namespace StudioElevenLib.Level5.Resource.RES
{
    public class RES : IResource
    {
        public string Name => "RES";

        private Dictionary<string, uint> StringTable { get; set; }

        public Dictionary<RESType, List<RESElement>> Items { get; set; }

        public RES()
        {

        }

        public RES(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                using (BinaryDataReader reader = new BinaryDataReader(memoryStream.ToArray()))
                {
                    byte[] data = ProcessAndValidateData(reader);
                    Initialize(data);
                }
            }
        }

        public RES(byte[] data)
        {
            using (BinaryDataReader reader = new BinaryDataReader(data))
            {
                byte[] validatedData = ProcessAndValidateData(reader);
                Initialize(validatedData);
            }
        }

        public RES(BinaryDataReader reader)
        {
            byte[] data = ProcessAndValidateData(reader);
            Initialize(data);
        }

        public void Save(string magic, string filepath)
        {
            byte[] data = Save(magic);
            File.WriteAllBytes(filepath, data);
        }

        public byte[] Save(string magic)
        {
            // Convert the magic into an 8-byte byte array
            byte[] magicBytes = ResourceHelper.ConvertMagicToBytes(magic);

            // Create the dictionary of strings with CRC32 and positions
            Dictionary<string, (uint crc32, int position)> stringDict = BuildStringDictionary();

            // Split the elements into two dictionaries
            Dictionary<RESType, List<RESElement>> materials = Items
                .Where(item => RESSupport.Materials.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value);

            Dictionary<RESType, List<RESElement>> nodes = Items
                .Where(item => RESSupport.Nodes.Contains(item.Key))
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
                        header.Magic = BitConverter.ToInt64(magicBytes, 0);
                        header.Unk1 = 1;
                        header.MaterialTableCount = (short)materials.Count;
                        header.NodeCount = (short)nodes.Count;

                        int headerPos = 20;
                        int dataPos = (materials.Count + nodes.Count) * 8;

                        // Material - Header table
                        if (materials.Count > 0)
                        {
                            writerDecomp.Seek(headerPos);
                            header._materialTableOffset = (short)(headerPos >> 2);

                            WriteSectionTable(materials, writerDecomp, writerData, ref headerPos, ref dataPos, stringDict);
                        }

                        // Node - Header table
                        if (nodes.Count > 0)
                        {
                            writerDecomp.Seek(headerPos);
                            header._nodeOffset = (short)(headerPos >> 2);

                            WriteSectionTable(nodes, writerDecomp, writerData, ref headerPos, ref dataPos, stringDict);
                        }

                        writerDecomp.Write(dataStream.ToArray());

                        // String table
                        header._stringOffset = (short)(writerDecomp.Position >> 2);
                        WriteStringTable(writerDecomp, stringDict);

                        writerDecomp.WriteAlignment(4);

                        writerDecomp.Seek(0);
                        writerDecomp.WriteStruct(header);
                    }

                    // Compress
                    writerComp.Write(new LZ10().Compress(memoryStream.ToArray()));
                }

                return fileStream.ToArray();
            }
        }

        private Dictionary<string, (uint crc32, int position)> BuildStringDictionary()
        {
            Encoding encoding = Encoding.GetEncoding(932); // Shift-JIS
            Dictionary<string, (uint, int)> stringDict = new Dictionary<string, (uint, int)>();
            int currentPosition = 0;

            foreach (var itemPair in Items)
            {
                foreach (var element in itemPair.Value)
                {
                    // Add the name of the element
                    AddStringToDictionary(element.Name, encoding, stringDict, ref currentPosition);

                    // If it is a ResMaterialData, process the images
                    if (element is ResMaterialData materialData)
                    {
                        foreach (var image in materialData.Images)
                        {
                            if (image != null && !string.IsNullOrEmpty(image.Name))
                            {
                                AddStringToDictionary(image.Name, encoding, stringDict, ref currentPosition);
                            }
                        }
                    }
                }
            }

            return stringDict;
        }

        private void AddStringToDictionary(string name, Encoding encoding, Dictionary<string, (uint, int)> dict, ref int position)
        {
            if (string.IsNullOrEmpty(name) || dict.ContainsKey(name))
                return;

            uint crc32 = Crc32.Compute(encoding.GetBytes(name));
            dict.Add(name, (crc32, position));

            position += encoding.GetByteCount(name) + 1;
        }

        private void WriteSectionTable(
            Dictionary<RESType, List<RESElement>> sections,
            BinaryDataWriter writerDecomp,
            BinaryDataWriter writerData,
            ref int headerPos,
            ref int dataPos,
            Dictionary<string, (uint crc32, int position)> stringDict)
        {
            foreach (var section in sections)
            {
                RESType resType = section.Key;
                List<RESElement> elements = section.Value;

                if (elements.Count == 0)
                    continue;

                // Write the structures of the elements in the data stream
                long dataStartPos = writerData.Position;

                foreach (var element in elements)
                {
                    object elementStruct = element.ToStruct(stringDict);
                    writerData.WriteStruct(elementStruct);
                }

                long dataEndPos = writerData.Position;
                int totalDataSize = (int)(dataEndPos - dataStartPos);
                int elementSize = totalDataSize / elements.Count;

                // Write the table header
                RESSupport.HeaderTable headerTable = new RESSupport.HeaderTable()
                {
                    _dataOffset = (short)(dataPos >> 2),
                    Count = (short)elements.Count,
                    Type = (short)resType,
                    Length = (short)elementSize
                };

                writerDecomp.WriteStruct(headerTable);

                headerPos += 8;
                dataPos += totalDataSize;
            }
        }

        private void WriteStringTable(BinaryDataWriter writer, Dictionary<string, (uint crc32, int position)> stringDict)
        {
            Encoding encoding = Encoding.GetEncoding(932); // Shift-JIS

            // Sort by position to write in the correct order
            var sortedStrings = stringDict.OrderBy(kvp => kvp.Value.position);

            foreach (var kvp in sortedStrings)
            {
                writer.Write(encoding.GetBytes(kvp.Key));
                writer.Write((byte)0x00);
            }
        }

        private byte[] ProcessAndValidateData(BinaryDataReader reader)
        {
            // Read the first 8 bytes to check the magic number
            long magicLong = reader.ReadValue<long>();
            string magicSTR = ResourceHelper.LongToUtf8String(magicLong);

            // Check if it is a valid RES format
            if (magicSTR.StartsWith("CHR") || magicSTR.StartsWith("ANMC00"))
            {
                // Valid format, reset position and return data
                reader.Seek(0);
                return reader.GetSection((int)reader.Length);
            }

            // The file is not recognized, try decompressing it.
            reader.Seek(0);
            byte[] compressedData = reader.GetSection((int)reader.Length);

            try
            {
                byte[] decompressedData = Compressor.Decompress(compressedData);

                // Check the magic after decompression
                using (BinaryDataReader decompReader = new BinaryDataReader(decompressedData))
                {
                    magicLong = decompReader.ReadValue<long>();
                    magicSTR = ResourceHelper.LongToUtf8String(magicLong);

                    if (magicSTR.StartsWith("CHR") || magicSTR.StartsWith("ANMC00"))
                    {
                        // Valid format after decompression
                        return decompressedData;
                    }
                }
            }
            catch
            {
                // Decompression failed
            }

            // Neither compressed nor uncompressed correspond to a valid RES format.
            throw new InvalidDataException("The file is not a valid RES file. Expected magic: 'CHR' or 'ANMC00'");
        }

        private void Initialize(byte[] data)
        {
            StringTable = new Dictionary<string, uint>();
            Items = new Dictionary<RESType, List<RESElement>>();

            // We assume data is decompressed
            using (BinaryDataReader reader = new BinaryDataReader(data))
            {
                RESSupport.Header header = reader.ReadStruct<RESSupport.Header>();

                LoadStringTable(reader, header.StringOffset);
                ReadSectionTable(reader, header.MaterialTableOffset, header.MaterialTableCount);
                ReadSectionTable(reader, header.NodeOffset, header.NodeCount);
            }
        }

        private void LoadStringTable(BinaryDataReader reader, int stringOffset)
        {
            reader.Seek(stringOffset);
            Encoding encoding = Encoding.GetEncoding(932); // Shift-JIS
            char[] separators = { '.', '_' };

            using (BinaryDataReader textReader = new BinaryDataReader(reader.GetSection((int)(reader.Length - reader.Position))))
            {
                while (textReader.Position < textReader.Length)
                {
                    string name = textReader.ReadString(encoding);

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        // Calculate the CRC32 for the full name
                        uint stringCrc32 = Crc32.Compute(encoding.GetBytes(name));

                        if (!StringTable.ContainsKey(name))
                        {
                            StringTable.Add(name, stringCrc32);
                        }

                        // Process the string recursively for the separate parts
                        ProcessStringRecursively(name, separators, encoding);
                    }
                }
            }
        }

        private void ProcessStringRecursively(string input, char[] separators, Encoding encoding)
        {
            // If the string is empty or null, return
            if (string.IsNullOrEmpty(input))
                return;

            foreach (char separator in separators)
            {
                if (input.Contains(separator.ToString()))
                {
                    // Split the string by the separator
                    string[] parts = input.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

                    // Add each part to the dictionary
                    foreach (string part in parts)
                    {
                        if (!string.IsNullOrWhiteSpace(part))
                        {
                            // Calculate the CRC32 for this part
                            uint crc32Part = Crc32.Compute(encoding.GetBytes(part));

                            // Add to dictionary if it does not exist
                            if (!StringTable.ContainsKey(part))
                            {
                                StringTable.Add(part, crc32Part);
                            }

                            // Recursive call on this part using the same separators
                            ProcessStringRecursively(part, separators, encoding);
                        }
                    }
                }
            }
        }

        private void ReadSectionTable(BinaryDataReader reader, int tableOffset, int tableCount)
        {
            for (int i = 0; i < tableCount; i++)
            {
                reader.Seek(tableOffset + i * 8);
                RESSupport.HeaderTable headerTable = reader.ReadStruct<RESSupport.HeaderTable>();

                RESType resType = (RESType)headerTable.Type;

                Console.WriteLine(resType + " type: " + headerTable.Type + " dataOffset: " + headerTable.DataOffset + " length: " + headerTable.Length + " count: " +  headerTable.Count);

                if (!Items.ContainsKey(resType))
                {
                    Items[resType] = new List<RESElement>();
                }

                if (headerTable.Count > 0)
                {
                    reader.Seek(headerTable.DataOffset);
                    Items[resType].AddRange(ReadElements(reader, resType, headerTable.Count));
                }
            }
        }

        private IEnumerable<RESElement> ReadElements(BinaryDataReader reader, RESType resType, int count)
        {
            switch (resType)
            {
                case RESType.Bone:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESBone.FromStruct(s, st));
                case RESType.Textproj:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESTextproj.FromStruct(s, st));
                case RESType.Properties:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESProperty.FromStruct(s, st));
                case RESType.Shading:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESShading.FromStruct(s, st));
                case RESType.Ref:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESRef.FromStruct(s, st));
                case RESType.Material1:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESMaterial1.FromStruct(s, st));
                case RESType.Material2:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESMaterial2.FromStruct(s, st));
                case RESType.MeshName:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESMesh.FromStruct(s, st));
                case RESType.TextureData:
                    return ReadTypedElements<RESTextureDataStruct>(reader, count,
                        (s, st) => RESTextureData.FromStruct(s, st));
                case RESType.MaterialData:
                    return ReadTypedElements<ResMaterialDataStruct>(reader, count,
                        (s, st) => ResMaterialData.FromStruct(s, st));
                case RESType.AnimationMTN2:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESAnimationMTN2.FromStruct(s, st));
                case RESType.AnimationIMN2:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESAnimationIMN2.FromStruct(s, st));
                case RESType.AnimationMTM2:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESAnimationMTM2.FromStruct(s, st));
                case RESType.MTNINF:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESAnimationMTNINF.FromStruct(s, st));
                case RESType.IMMINF:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESAnimationIMMINF.FromStruct(s, st));
                case RESType.MTMINF:
                    return ReadTypedElements<ResElementStruct>(reader, count,
                        (s, st) => RESAnimationMTMINF.FromStruct(s, st));
                default:
                    return Enumerable.Empty<RESElement>();
            }
        }

        private IEnumerable<RESElement> ReadTypedElements<TStruct>(
            BinaryDataReader reader,
            int count,
            Func<TStruct, Dictionary<string, uint>, RESElement> converter)
        {
            var structs = reader.ReadMultipleStruct<TStruct>(count);
            return structs.Select(s => converter(s, StringTable));
        }
    }
}