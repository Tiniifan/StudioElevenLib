using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression.NoCompression;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Resource.Types.Scene3D;

namespace StudioElevenLib.Level5.Resource.XRES
{
    /// <summary>
    /// Handles the encoding and saving of Level-5 XRES files.
    /// </summary>
    public class XRESWriter
    {
        private readonly XRES _xres;

        /// <summary>
        /// Initializes a new instance of the XRESWriter with the target XRES object.
        /// </summary>
        public XRESWriter(XRES xres)
        {
            _xres = xres ?? throw new ArgumentNullException(nameof(xres));
        }

        /// <summary>
        /// Encodes an XRES resource into its binary format and saves the result to a file.
        /// </summary>
        public void Save(string magic, string filepath)
        {
            byte[] data = Save(magic);
            File.WriteAllBytes(filepath, data);
        }

        /// <summary>
        /// Encodes an XRES resource into its binary format and returns the file bytes.
        /// </summary>
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

                            if (_xres.Items.ContainsKey(resType) && _xres.Items[resType].Count > 0)
                            {
                                int elementSize = XRESSupport.TypeLength.ContainsKey(resType)
                                    ? XRESSupport.TypeLength[resType]
                                    : 8;
                                currentDataOffset += _xres.Items[resType].Count * elementSize;
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
                            short count = (short)(_xres.Items.ContainsKey(resType) ? _xres.Items[resType].Count : 0);

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
                            if (_xres.Items.ContainsKey(resType) && _xres.Items[resType].Count > 0)
                            {
                                foreach (var element in _xres.Items[resType])
                                {
                                    if (element is XRESTextureData xtexture)
                                    {
                                        var elementStruct = xtexture.ToStruct(stringDict);
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
                if (_xres.Items.ContainsKey(resType))
                {
                    foreach (var element in _xres.Items[resType])
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
    }
}