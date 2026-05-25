using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Resource.Types.Scene3D;

namespace StudioElevenLib.Level5.Resource.XRES
{
    /// <summary>
    /// Handles the reading and decoding of Level-5 XRES files.
    /// </summary>
    public class XRESReader
    {
        private readonly byte[] _data;
        private readonly BinaryDataReader _dataReader;

        private Dictionary<string, uint> _stringTable;
        private Dictionary<RESType, List<RESElement>> _items;

        /// <summary>
        /// Initializes a new instance of the XRESReader with a byte array.
        /// </summary>
        public XRESReader(byte[] data)
        {
            _data = data;
        }

        /// <summary>
        /// Initializes a new instance of the XRESReader with a BinaryDataReader.
        /// </summary>
        public XRESReader(BinaryDataReader dataReader)
        {
            _dataReader = dataReader;
        }

        /// <summary>
        /// Reads the XRES data and extracts the elements and string table.
        /// </summary>
        public (Dictionary<RESType, List<RESElement>> items, Dictionary<string, uint> stringTable) Read()
        {
            bool disposeReader = false;
            BinaryDataReader readerToUse = _dataReader;

            if (_data != null)
            {
                readerToUse = new BinaryDataReader(_data);
                disposeReader = true;
            }

            try
            {
                byte[] validatedData = ProcessAndValidateData(readerToUse);
                return Initialize(validatedData);
            }
            finally
            {
                if (disposeReader)
                {
                    readerToUse?.Dispose();
                }
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

        private (Dictionary<RESType, List<RESElement>> items, Dictionary<string, uint> stringTable) Initialize(byte[] data)
        {
            _stringTable = new Dictionary<string, uint>();
            _items = new Dictionary<RESType, List<RESElement>>();

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
                ReadSection(reader, header.LookUpTable, RESType.LookUpTable);
                ReadSection(reader, header.Properties, RESType.Properties);
                ReadSection(reader, header.MTNINF, RESType.MTNINF);
                ReadSection(reader, header.IMMINF, RESType.IMMINF);
                ReadSection(reader, header.MTMINF, RESType.MTMINF);
                ReadSection(reader, header.Textproj, RESType.Textproj);
            }

            return (_items, _stringTable);
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

                        if (!_stringTable.ContainsKey(name))
                        {
                            _stringTable.Add(name, stringCrc32);
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
                            if (!_stringTable.ContainsKey(part))
                            {
                                _stringTable.Add(part, crc32Part);
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

            if (!_items.ContainsKey(resType))
            {
                _items[resType] = new List<RESElement>();
            }

            reader.Seek(headerTable.DataOffset);
            _items[resType].AddRange(ReadElements(reader, resType, headerTable.Count));
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
                case RESType.LookUpTable:
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
            return structs.Select(s => converter(s, _stringTable));
        }
    }
}