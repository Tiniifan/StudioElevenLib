using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Compression.NoCompression;
using StudioElevenLib.Level5.Resource.Types.Scene3D;

namespace StudioElevenLib.Level5.Resource.XRES
{
    public class XRES : IResource
    {
        public string Name => "XRES";

        private Dictionary<string, uint> StringTable { get; set; }

        public Dictionary<RESType, List<RESElement>> Items { get; set; }

        public XRES()
        {

        }

        public XRES(Stream stream)
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

        public XRES(byte[] data)
        {
            using (BinaryDataReader reader = new BinaryDataReader(data))
            {
                byte[] validatedData = ProcessAndValidateData(reader);
                Initialize(validatedData);
            }
        }

        public XRES(BinaryDataReader reader)
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

            using (MemoryStream fileStream = new MemoryStream())
            {
                BinaryDataWriter writerComp = new BinaryDataWriter(fileStream);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                    using (MemoryStream dataStream = new MemoryStream())
                    {
                        BinaryDataWriter writerData = new BinaryDataWriter(dataStream);

                        // Calculate data offsets for each type in the defined order
                        Dictionary<RESType, int> dataOffsetDict = new Dictionary<RESType, int>();
                        int currentDataOffset = 0x64; // Header size for XRES

                        foreach (RESType resType in XRESSupport.DataOrder)
                        {
                            dataOffsetDict[resType] = currentDataOffset;

                            if (Items.ContainsKey(resType) && Items[resType].Count > 0)
                            {
                                int elementSize = XRESSupport.TypeLength.ContainsKey(resType)
                                    ? XRESSupport.TypeLength[resType]
                                    : 8;
                                currentDataOffset += Items[resType].Count * elementSize;
                            }
                        }

                        // Write header
                        writerDecomp.Write(BitConverter.ToInt32(magicBytes, 0)); // Magic (4 bytes for XRES)
                        writerDecomp.Write((short)currentDataOffset); // String offset
                        writerDecomp.Write((short)1); // Unk1
                        writerDecomp.Write(new byte[0x0C]); // Empty block

                        // Write header tables in the specific XRES order
                        foreach (RESType resType in XRESSupport.TypeOrder)
                        {
                            short dataOffset = (short)(dataOffsetDict.ContainsKey(resType) ? dataOffsetDict[resType] : 0);
                            short count = (short)(Items.ContainsKey(resType) ? Items[resType].Count : 0);

                            writerDecomp.Write(dataOffset);
                            writerDecomp.Write(count);

                            // Special case: Material1 and Material2 have an additional empty int
                            if (resType == RESType.Material1 || resType == RESType.Material2)
                            {
                                writerDecomp.Write(0);
                            }
                        }

                        // Write data in the specific XRES data order
                        foreach (RESType resType in XRESSupport.DataOrder)
                        {
                            if (Items.ContainsKey(resType) && Items[resType].Count > 0)
                            {
                                foreach (var element in Items[resType])
                                {
                                    object elementStruct = element.ToStruct(stringDict);
                                    writerData.WriteStruct(elementStruct);
                                }
                            }
                        }

                        writerDecomp.Write(dataStream.ToArray());

                        // String table
                        WriteStringTable(writerDecomp, stringDict);

                        writerDecomp.WriteAlignment(4);
                    }

                    // Compress with NoCompression
                    writerComp.Write(new NoCompression().Compress(memoryStream.ToArray()));
                }

                return fileStream.ToArray();
            }
        }

        private Dictionary<string, (uint crc32, int position)> BuildStringDictionary()
        {
            Encoding encoding = Encoding.GetEncoding(932); // Shift-JIS
            Dictionary<string, (uint, int)> stringDict = new Dictionary<string, (uint, int)>();
            int currentPosition = 0;

            // Process types in XRES order to maintain consistency
            foreach (RESType resType in XRESSupport.TypeOrder)
            {
                if (Items.ContainsKey(resType))
                {
                    foreach (var element in Items[resType])
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

            // Check if it is a valid XRES format
            if (magicSTR.StartsWith("XRES") || magicSTR.StartsWith("XA01"))
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

                    if (magicSTR.StartsWith("XRES") || magicSTR.StartsWith("XA01"))
                    {
                        // Valid format after decompression
                        return compressedData;
                    }
                }
            }
            catch
            {
                // Decompression failed
            }

            // Neither compressed nor uncompressed correspond to a valid XRES format.
            throw new InvalidDataException("The file is not a valid XRES file. Expected magic: 'XRES' or 'XA01'");
        }

        private void Initialize(byte[] data)
        {
            StringTable = new Dictionary<string, uint>();
            Items = new Dictionary<RESType, List<RESElement>>();

            // We assume data is decompressed
            using (BinaryDataReader reader = new BinaryDataReader(data))
            {
                XRESSupport.Header header = reader.ReadStruct<XRESSupport.Header>();

                LoadStringTable(reader, header.StringOffset);

                // Read all sections in XRES order
                ReadSection(reader, header.MaterialTypeUnk1, RESType.MaterialTypeUnk1);
                ReadSection(reader, header.Material1, RESType.Material1);
                ReadSection(reader, header.Material2, RESType.Material2);
                ReadSection(reader, header.TextureData, RESType.TextureData);
                ReadSection(reader, header.MaterialTypeUnk2, RESType.MaterialTypeUnk2);
                ReadSection(reader, header.MaterialData, RESType.MaterialData);
                ReadSection(reader, header.MeshName, RESType.MeshName);
                ReadSection(reader, header.Bone, RESType.Bone);
                ReadSection(reader, header.AnimationMTN2, RESType.AnimationMTN2);
                ReadSection(reader, header.AnimationIMN2, RESType.AnimationIMN2);
                ReadSection(reader, header.AnimationMTM2, RESType.AnimationMTM2);
                ReadSection(reader, header.Shading, RESType.Shading);
                ReadSection(reader, header.NodeTypeUnk1, RESType.NodeTypeUnk1);
                ReadSection(reader, header.Properties, RESType.Properties);
                ReadSection(reader, header.MTNINF, RESType.MTNINF);
                ReadSection(reader, header.IMMINF, RESType.IMMINF);
                ReadSection(reader, header.MTMINF, RESType.MTMINF);
                ReadSection(reader, header.Textproj, RESType.Textproj);
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

        private void ReadSection(BinaryDataReader reader, XRESSupport.HeaderTable headerTable, RESType resType)
        {
            if (headerTable.Count == 0)
                return;

            if (!Items.ContainsKey(resType))
            {
                Items[resType] = new List<RESElement>();
            }

            reader.Seek(headerTable.DataOffset);
            Items[resType].AddRange(ReadElements(reader, resType, headerTable.Count));
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
                    // XRES uses XRESTextureDataStruct instead of RESTextureDataStruct
                    return ReadTypedElements<XRESTextureDataStruct>(reader, count,
                        (s, st) => XRESTextureData.FromStruct(s, st));
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