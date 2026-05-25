using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression.LZ10;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Resource.Types.Scene3D;

namespace StudioElevenLib.Level5.Resource.RES
{
    /// <summary>
    /// Handles the encoding and saving of Level-5 RES files.
    /// </summary>
    public class RESWriter
    {
        private readonly RES _res;

        /// <summary>
        /// Initializes a new instance of the RESWriter with the target RES object.
        /// </summary>
        public RESWriter(RES res)
        {
            _res = res ?? throw new ArgumentNullException(nameof(res));
        }

        /// <summary>
        /// Encodes a RES resource into its binary format and saves the result to a file.
        /// </summary>
        public void Save(string magic, string filepath)
        {
            byte[] data = Save(magic);
            File.WriteAllBytes(filepath, data);
        }

        /// <summary>
        /// Encodes a RES resource into its binary format and returns the file bytes.
        /// </summary>
        public byte[] Save(string magic)
        {
            // Convert the magic into an 8-byte byte array
            byte[] magicBytes = ResourceHelper.ConvertMagicToBytes(magic);

            // Create the dictionary of strings with CRC32 and positions
            Dictionary<string, (uint crc32, int position)> stringDict = BuildStringDictionary();

            // Split the elements into two dictionaries
            Dictionary<RESType, List<RESElement>> materials = _res.Items
                .Where(item => RESSupport.Materials.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value);

            Dictionary<RESType, List<RESElement>> nodes = _res.Items
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
                        int dataPos = headerPos + (materials.Count + nodes.Count) * 8;

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

            foreach (var itemPair in _res.Items)
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
                    if (element is RESTextureData texture)
                    {
                        var elementStruct = texture.ToStruct(stringDict);
                        writerData.WriteStruct(elementStruct);
                    }
                    else if (element is ResMaterialData resMaterialData)
                    {
                        var elementStruct = resMaterialData.ToStruct(stringDict);
                        writerData.WriteStruct(elementStruct);
                    }
                    else
                    {
                        var elementStruct = element.ToStruct(stringDict);
                        writerData.WriteStruct(elementStruct);
                    }
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
    }
}