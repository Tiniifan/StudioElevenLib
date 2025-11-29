using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Resource;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Resource.Types.Scene3D;

namespace StudioElevenLib.Level5.Resource.RES
{
    public class RES : IResource
    {
        public string Name => "RES";

        private Dictionary<string, uint> StringTable { get; set; }

        public Dictionary<RESType, List<RESElement>> Items { get; set; }

        public RES(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                Initialize(memoryStream.ToArray());
            }
        }

        public RES(byte[] data)
        {
            Initialize(data);
        }

        public void Save(string filepath)
        {

        }

        public byte[] Save(byte[] magic)
        {
            //// Split Items into 2 dictionaries
            //Dictionary<RESType, List<byte[]>> materials = Items.Where(item => RESSupport.Materials.Contains(item.Key))
            //    .ToDictionary(item => item.Key, item => item.Value);
            //Dictionary<RESType, List<byte[]>> nodes = Items.Where(item => RESSupport.Nodes.Contains(item.Key))
            //    .ToDictionary(item => item.Key, item => item.Value);

            //using (MemoryStream fileStream = new MemoryStream())
            //{
            //    BinaryDataWriter writerComp = new BinaryDataWriter(fileStream);

            //    using (MemoryStream memoryStream = new MemoryStream())
            //    {
            //        BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

            //        using (MemoryStream dataStream = new MemoryStream())
            //        {
            //            BinaryDataWriter writerData = new BinaryDataWriter(dataStream);

            //            RESSupport.Header header = new RESSupport.Header();
            //            header.Magic = BitConverter.ToInt64(magic, 0);
            //            header.Unk1 = 1;
            //            header.MaterialTableCount = (short)materials.Count;
            //            header.NodeCount = (short)nodes.Count;

            //            int headerPos = 20;
            //            int dataPos = Items.Count * 8;

            //            // Material - Header table
            //            if (materials.Count > 0)
            //            {
            //                writerDecomp.Seek(headerPos);
            //                header._materialTableOffset = (short)(headerPos >> 2);

            //                for (int i = 0; i < materials.Count; i++)
            //                {
            //                    KeyValuePair<RESType, List<byte[]>> resType = materials.ElementAt(i);

            //                    RESSupport.HeaderTable materialHeaderTable = new RESSupport.HeaderTable()
            //                    {
            //                        _dataOffset = (short)(dataPos >> 2),
            //                        Count = (short)resType.Value.Count,
            //                        Type = (short)resType.Key,
            //                        Length = (short)resType.Value[0].Length
            //                    };

            //                    headerPos += 8;
            //                    dataPos += resType.Value.Sum(byteArray => byteArray.Length);

            //                    writerDecomp.WriteStruct(materialHeaderTable);
            //                    writerData.Write(resType.Value.SelectMany(bytes => bytes).ToArray());
            //                }
            //            }

            //            // Node - Header table
            //            if (nodes.Count > 0)
            //            {
            //                writerDecomp.Seek(headerPos);
            //                header._nodeOffset = (short)(headerPos >> 2);

            //                for (int i = 0; i < nodes.Count; i++)
            //                {
            //                    KeyValuePair<RESType, List<byte[]>> resType = nodes.ElementAt(i);

            //                    RESSupport.HeaderTable nodeHeaderTable = new RESSupport.HeaderTable()
            //                    {
            //                        _dataOffset = (short)(dataPos >> 2),
            //                        Count = (short)resType.Value.Count,
            //                        Type = (short)resType.Key,
            //                        Length = (short)resType.Value[0].Length
            //                    };

            //                    headerPos += 8;
            //                    dataPos += resType.Value.Sum(byteArray => byteArray.Length);

            //                    writerDecomp.WriteStruct(nodeHeaderTable);
            //                    writerData.Write(resType.Value.SelectMany(bytes => bytes).ToArray());
            //                }
            //            }

            //            writerDecomp.Write(dataStream.ToArray());

            //            // String table
            //            header._stringOffset = (short)(writerDecomp.Position>>2);
            //            for (int i = 0; i < StringTable.Count; i++)
            //            {
            //                writerDecomp.Write(Encoding.GetEncoding(932).GetBytes(StringTable[i]));
            //                writerDecomp.Write((byte)0);
            //            }

            //            writerDecomp.WriteAlignment(4);

            //            writerDecomp.Seek(0);
            //            writerDecomp.WriteStruct(header);
            //        }

            //        // Compress
            //       writerComp.Write(memoryStream.ToArray());
            //    }

            //    return fileStream.ToArray();
            //}

            return null;
        }

        private void Initialize(byte[] data)
        {
            StringTable = new Dictionary<string, uint>();
            Items = new Dictionary<RESType, List<RESElement>>();

            using (BinaryDataReader reader = new BinaryDataReader(Compressor.Decompress(data)))
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
                        // Calculer le CRC32 pour le nom complet
                        uint stringCrc32 = Crc32.Compute(encoding.GetBytes(name));

                        if (!StringTable.ContainsKey(name))
                        {
                            StringTable.Add(name, stringCrc32);
                        }

                        // Traiter la chaîne de manière récursive pour les parties séparées
                        ProcessStringRecursively(name, separators, encoding);
                    }
                }
            }
        }

        private void ProcessStringRecursively(string input, char[] separators, Encoding encoding)
        {
            // Si la chaîne est vide ou nulle, on retourne
            if (string.IsNullOrEmpty(input))
                return;

            foreach (char separator in separators)
            {
                if (input.Contains(separator.ToString()))
                {
                    // Diviser la chaîne par le séparateur
                    string[] parts = input.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

                    // Ajouter chaque partie au dictionnaire
                    foreach (string part in parts)
                    {
                        if (!string.IsNullOrWhiteSpace(part))
                        {
                            // Calculer le CRC32 pour cette partie
                            uint crc32Part = Crc32.Compute(encoding.GetBytes(part));

                            // Ajouter au dictionnaire si inexistant
                            if (!StringTable.ContainsKey(part))
                            {
                                StringTable.Add(part, crc32Part);
                            }

                            // Appel récursif sur cette partie en utilisant les mêmes séparateurs
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