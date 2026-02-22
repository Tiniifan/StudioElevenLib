using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StudioElevenLib.Level5.Binary.Logic;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Tools;
using StudioElevenLib.Collections;
using System.Linq;
using System.Runtime.InteropServices;

namespace StudioElevenLib.Level5.Binary
{
    /// <summary>
    /// Represents a configuration binary file (.cfg), providing methods for parsing and saving the data.
    /// </summary>
    public class CfgBin<TNode> where TNode : TreeNode<Entry>
    {
        /// <summary>
        /// Gets or sets the encoding used for reading string data.
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets the root entry node for entries in the configuration.
        /// </summary>
        public TNode Entries { get; protected set; }

        /// <summary>
        /// Gets or sets the dictionary of strings found in the configuration file.
        /// </summary>
        public Dictionary<int, string> Strings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgBin"/> class.
        /// </summary>
        public CfgBin()
        {
            Entries = CreateRootNode();
            Strings = new Dictionary<int, string>();
        }

        protected virtual TNode CreateRootNode()
        {
            return (TNode)Activator.CreateInstance(typeof(TNode), new object[] { new Entry("ROOT"), 0 });
        }

        protected virtual TNode CreateNode(Entry entry, int level)
        {
            return (TNode)Activator.CreateInstance(typeof(TNode), new object[] { entry, level });
        }

        /// <summary>
        /// Opens and parses the configuration binary data from a byte array.
        /// </summary>
        /// <param name="data">The byte array representing the binary data of the configuration file.</param>
        public void Open(byte[] data)
        {
            using (var reader = new BinaryDataReader(data))
            {
                ParseData(reader);
            }
        }

        /// <summary>
        /// Opens and parses the configuration binary data from a stream.
        /// </summary>
        /// <param name="stream">The stream representing the binary data of the configuration file.</param>
        public void Open(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream))
            {
                ParseData(reader);
            }
        }

        /// <summary>
        /// Parses the binary data using the provided <see cref="BinaryDataReader"/>.
        /// </summary>
        /// <param name="reader">The reader to parse the binary data.</param>
        private void ParseData(BinaryDataReader reader)
        {
            // Move to encoding offset
            reader.Seek((uint)reader.Length - 0x0A);

            // Get encoding
            Encoding = reader.ReadValue<byte>() == 0 ? Encoding.GetEncoding("SHIFT-JIS") : Encoding.UTF8;

            reader.Seek(0x0);
            var header = reader.ReadStruct<CfgBinSupport.Header>();

            byte[] entriesBuffer = reader.GetSection(0x10, header.StringTableOffset);

            byte[] stringTableBuffer = reader.GetSection((uint)header.StringTableOffset, header.StringTableLength);
            Strings = ParseStrings(header.StringTableCount, stringTableBuffer);

            long keyTableOffset = RoundUp(header.StringTableOffset + header.StringTableLength, 16);
            reader.Seek((uint)keyTableOffset);
            int keyTableSize = reader.ReadValue<int>();
            byte[] keyTableBlob = reader.GetSection((uint)keyTableOffset, keyTableSize);
            Dictionary<uint, string> keyTable = ParseKeyTable(keyTableBlob);

            List<Entry> entries = ParseEntries(header.EntriesCount, entriesBuffer, keyTable);

            Entries = ProcessEntries(entries);
        }

        /// <summary>
        /// Saves the current cfgbin to the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to save the cfgbin to.</param>
        public void Save(string fileName)
        {
            var data = Save();
            File.WriteAllBytes(fileName, data);
        }

        /// <summary>
        /// Serializes the current cfgbin to a byte array.
        /// </summary>
        /// <returns>A byte array representing the file.</returns>
        public byte[] Save()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryDataWriter(stream))
                {
                    // Collect all entries from the tree
                    var allEntries = new List<Entry>();
                    CollectEntries(Entries, allEntries);

                    // Create the unique string table
                    var stringTable = BuildStringTable(allEntries);
                    var stringTableBytes = SerializeStringTable(stringTable);

                    // Create the key table (CRC32)
                    var keyTable = BuildKeyTable(allEntries);
                    var keyTableBytes = SerializeKeyTable(keyTable);

                    // Serialize entries
                    var entriesBytes = SerializeEntries(allEntries, stringTable, keyTable);

                    CfgBinSupport.Header header;
                    header.EntriesCount = allEntries.Count;
                    header.StringTableOffset = 0;
                    header.StringTableLength = 0;
                    header.StringTableCount = stringTable.Count;

                    writer.Seek(0x10);

                    writer.Write(entriesBytes);

                    writer.WriteAlignment(0x10, 0xFF);
                    header.StringTableOffset = (int)writer.Position;

                    if (stringTable.Count > 0)
                    {
                        writer.Write(stringTableBytes);
                        header.StringTableLength = (int)writer.Position - header.StringTableOffset;
                        writer.WriteAlignment(0x10, 0xFF);
                    }

                    writer.Write(keyTableBytes);

                    writer.Write(new byte[5] { 0x01, 0x74, 0x32, 0x62, 0xFE });
                    writer.Write(new byte[4] { 0x01, GetEncoding(), 0x00, 0x01 });
                    writer.WriteAlignment();

                    writer.Seek(0);
                    writer.WriteStruct(header);

                    return stream.ToArray();
                }
            }
        }

        public byte GetEncoding()
        {
            if (Encoding != null && Encoding.Equals(Encoding.GetEncoding("SHIFT-JIS")))
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Parses the strings from the string table buffer.
        /// </summary>
        /// <param name="stringCount">The number of strings in the string table.</param>
        /// <param name="stringTableBuffer">The byte buffer containing the string table.</param>
        /// <returns>A dictionary mapping string offsets to string values.</returns>
        private Dictionary<int, string> ParseStrings(int stringCount, byte[] stringTableBuffer)
        {
            var result = new Dictionary<int, string>();

            using (var reader = new BinaryDataReader(stringTableBuffer))
            {
                for (int i = 0; i < stringCount; i++)
                {
                    if (!result.ContainsKey((int)reader.Position))
                    {
                        result.Add((int)reader.Position, reader.ReadString(Encoding));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Parses the key table from the provided buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer containing the key table.</param>
        /// <returns>A dictionary mapping CRC32 values to strings.</returns>
        private Dictionary<uint, string> ParseKeyTable(byte[] buffer)
        {
            var keyTable = new Dictionary<uint, string>();

            using (var reader = new BinaryDataReader(buffer))
            {
                var header = reader.ReadStruct<CfgBinSupport.KeyHeader>();
                byte[] keyStringBlob = reader.GetSection((uint)header.KeyStringOffset, header.keyStringLength);

                for (int i = 0; i < header.KeyCount; i++)
                {
                    uint crc32 = reader.ReadValue<uint>();
                    int stringStart = reader.ReadValue<int>();
                    int stringEnd = Array.IndexOf(keyStringBlob, (byte)0, stringStart);
                    byte[] stringBuf = new byte[stringEnd - stringStart];
                    Array.Copy(keyStringBlob, stringStart, stringBuf, 0, stringEnd - stringStart);
                    string key = Encoding.GetString(stringBuf);
                    keyTable[crc32] = key;
                }
            }

            return keyTable;
        }

        /// <summary>
        /// Parses the entries from the buffer.
        /// </summary>
        /// <param name="entriesCount">The number of entries to parse.</param>
        /// <param name="entriesBuffer">The buffer containing the entries.</param>
        /// <param name="keyTable">A dictionary of keys used for resolving entry names.</param>
        /// <returns>A list of parsed entries.</returns>
        private List<Entry> ParseEntries(int entriesCount, byte[] entriesBuffer, Dictionary<uint, string> keyTable)
        {
            var temp = new List<Entry>();
            using (var reader = new BinaryDataReader(entriesBuffer))
            {
                for (int i = 0; i < entriesCount; i++)
                {
                    uint crc32 = reader.ReadValue<uint>();
                    string name = keyTable[crc32];
                    int paramCount = reader.ReadValue<byte>();
                    CfgValueType[] paramTypes = new CfgValueType[paramCount];
                    int paramIndex = 0;
                    for (int j = 0; j < (int)Math.Ceiling((double)paramCount / 4); j++)
                    {
                        byte paramType = reader.ReadValue<byte>();
                        for (int k = 0; k < 4; k++)
                        {
                            if (paramIndex < paramTypes.Length)
                            {
                                int tag = (paramType >> (2 * k)) & 3;

                                // Remplacement du switch expression par un switch classique
                                switch (tag)
                                {
                                    case 0:
                                        paramTypes[paramIndex] = CfgValueType.String;
                                        break;
                                    case 1:
                                        paramTypes[paramIndex] = CfgValueType.Int;
                                        break;
                                    case 2:
                                        paramTypes[paramIndex] = CfgValueType.Float;
                                        break;
                                    default:
                                        paramTypes[paramIndex] = CfgValueType.Unknown;
                                        break;
                                }
                                paramIndex++;
                            }
                        }
                    }
                    if ((Math.Ceiling((double)paramCount / 4) + 1) % 4 != 0)
                    {
                        reader.Seek((uint)(reader.Position + 4 - (reader.Position % 4)));
                    }
                    var variables = new List<Variable>();
                    for (int j = 0; j < paramCount; j++)
                    {
                        if (paramTypes[j] == CfgValueType.String)
                        {
                            int offset = reader.ReadValue<int>();
                            string text = offset != -1 && Strings.ContainsKey(offset) ? Strings[offset] : null;
                            variables.Add(new Variable(CfgValueType.String, text));
                        }
                        else if (paramTypes[j] == CfgValueType.Int)
                        {
                            variables.Add(new Variable(CfgValueType.Int, reader.ReadValue<int>()));
                        }
                        else if (paramTypes[j] == CfgValueType.Float)
                        {
                            variables.Add(new Variable(CfgValueType.Float, reader.ReadValue<float>()));
                        }
                        else
                        {
                            variables.Add(new Variable(CfgValueType.Unknown, reader.ReadValue<int>()));
                        }
                    }
                    temp.Add(new Entry(name, variables));
                }
            }
            return temp;
        }

        /// <summary>
        /// Processes the parsed entries and arranges them into a hierarchical tree structure.
        /// </summary>
        /// <param name="entries">The list of parsed entries.</param>
        /// <returns>The root node of the tree structure representing the entries.</returns>
        private TNode ProcessEntries(List<Entry> entries)
        {
            var root = (TNode)CreateRootNode(); // cast explicite
            var currentNode = root;

            foreach (var entry in entries)
            {
                string entryName = entry.Name;
                if (string.IsNullOrEmpty(entryName))
                    continue;

                if (currentNode != null)
                {
                    if (entryName.EndsWith("_BEGIN") || entryName.EndsWith("LIST_BEG") || entryName == "PTREE")
                    {
                        var child = (TNode)CreateNode(entry, currentNode.Level + 1); // cast explicite
                        currentNode.AddChild(child);
                        currentNode = child;
                    }
                    else if (entryName.EndsWith("_END") || entryName.EndsWith("LIST_END") || entryName == "_PTREE")
                    {
                        if (currentNode.Parent != null)
                            currentNode = (TNode)currentNode.Parent; // cast explicite si Parent est TreeNode<Entry>
                    }
                    else
                    {
                        var child = (TNode)CreateNode(entry, currentNode.Level + 1); // cast explicite
                        currentNode.AddChild(child);
                    }
                }
            }

            return root;
        }

        /// <summary>
        /// Rounds up a number to the nearest multiple of a specified exponent.
        /// </summary>
        /// <param name="n">The number to round up.</param>
        /// <param name="exp">The exponent to round up to (e.g., 16 for 16-byte alignment).</param>
        /// <returns>The rounded-up number.</returns>
        private long RoundUp(int n, int exp)
        {
            return ((n + exp - 1) / exp) * exp;
        }

        /// <summary>
        /// Collecte récursivement toutes les entrées de l'arbre
        /// </summary>
        private void CollectEntries(TNode node, List<Entry> entries)
        {
            if (node == null || node.Item == null) return;

            // Ne pas inclure l'entrée ROOT
            if (node.Item.Name != "ROOT")
            {
                entries.Add(node.Item);
            }

            foreach (var child in node.Children)
            {
                CollectEntries((TNode)child, entries);
            }

            if (node.Item.Name == "PTREE")
            {
                entries.Add(new Entry("_PTREE"));
            }
        }

        /// <summary>
        /// Construit la table des chaînes uniques
        /// </summary>
        private Dictionary<string, int> BuildStringTable(List<Entry> entries)
        {
            var uniqueStrings = new HashSet<string>();

            foreach (var entry in entries)
            {
                if (entry.Variables != null)
                {
                    foreach (var variable in entry.Variables)
                    {
                        if (variable.Type == CfgValueType.String && variable.Value is string str && !string.IsNullOrEmpty(str))
                        {
                            uniqueStrings.Add(str);
                        }
                    }
                }
            }

            var stringTable = new Dictionary<string, int>();
            int offset = 0;

            foreach (var str in uniqueStrings)
            {
                stringTable.Add(str, offset);
                offset += Encoding.GetByteCount(str) + 1; // +1 for the end character 0x00
            }

            return stringTable;
        }

        /// <summary>
        /// Sérialise la table des chaînes
        /// </summary>
        private byte[] SerializeStringTable(Dictionary<string, int> stringTable)
        {
            using (var stream = new MemoryStream())
            {
                // Trier par offset pour maintenir l'ordre
                var sortedStrings = stringTable.OrderBy(kvp => kvp.Value);

                foreach (var kvp in sortedStrings)
                {
                    byte[] stringBytes = Encoding.GetBytes(kvp.Key);
                    stream.Write(stringBytes, 0, stringBytes.Length);
                    stream.WriteByte(0x00); // Caractère de fin
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Construit la table des clés avec leurs CRC32
        /// </summary>
        private Dictionary<string, uint> BuildKeyTable(List<Entry> entries)
        {
            var keyTable = new Dictionary<string, uint>();

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Name) && !keyTable.ContainsKey(entry.Name))
                {
                    uint crc32 = Crc32.Compute(Encoding.GetBytes(entry.Name));
                    keyTable[entry.Name] = crc32;
                }
            }

            return keyTable;
        }

        /// <summary>
        /// Sérialise la table des clés
        /// </summary>
        private byte[] SerializeKeyTable(Dictionary<string, uint> keyTable)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryDataWriter writer = new BinaryDataWriter(stream))
            {
                // Calculate the total size required for the header and key strings
                uint headerSize = (uint)Marshal.SizeOf(typeof(CfgBinSupport.KeyHeader));
                uint keyStringsSize = 0;

                foreach (var key in keyTable.Keys)
                {
                    keyStringsSize += (uint)Encoding.GetByteCount(key) + 1; // +1 for null-terminator
                }

                // Write header
                var header = new CfgBinSupport.KeyHeader
                {
                    KeyCount = keyTable.Count,
                    keyStringLength = (int)keyStringsSize
                };

                writer.Seek(0x10);

                int stringOffset = 0;

                // Calculate CRC32 for each key and write key entries
                foreach (KeyValuePair<string, uint> myKey in keyTable)
                {
                    uint crc32 = myKey.Value;
                    writer.Write(crc32);
                    writer.Write(stringOffset);
                    stringOffset += Encoding.GetByteCount(myKey.Key) + 1;
                }

                writer.WriteAlignment(0x10, 0xFF);

                header.KeyStringOffset = (int)writer.Position;

                // Write key strings
                foreach (var key in keyTable.Keys)
                {
                    byte[] stringBytes = Encoding.GetBytes(key);
                    writer.Write(stringBytes);
                    writer.Write((byte)0); // Null-terminator
                }

                writer.WriteAlignment(0x10, 0xFF);
                header.KeyLength = (int)writer.Position;
                writer.Seek(0x00);
                writer.WriteStruct(header);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Sérialise les entrées
        /// </summary>
        private byte[] SerializeEntries(List<Entry> entries, Dictionary<string, int> stringTable, Dictionary<string, uint> keyTable)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryDataWriter(stream))
            {
                foreach (var entry in entries)
                {
                    // CRC32 of the entry name
                    writer.Write(keyTable[entry.Name]);

                    // Number of parameters
                    int paramCount = entry.Variables?.Count ?? 0;
                    writer.Write((byte)paramCount);

                    int typeByteCount = (int)Math.Ceiling((double)paramCount / 4);
                    List<byte> typeBytes = new List<byte>();

                    for (int i = 0; i < typeByteCount; i++)
                    {
                        byte typeByte = 0;

                        for (int j = 0; j < 4 && (i * 4 + j) < paramCount; j++)
                        {
                            var variable = entry.Variables[i * 4 + j];
                            int typeValue;
                            switch (variable.Type)
                            {
                                case CfgValueType.String:
                                    typeValue = 0;
                                    break;
                                case CfgValueType.Int:
                                    typeValue = 1;
                                    break;
                                case CfgValueType.Float:
                                    typeValue = 2;
                                    break;
                                default:
                                    typeValue = 0;
                                    break;
                            }
                            typeByte |= (byte)(typeValue << (2 * j));
                        }

                        typeBytes.Add(typeByte);
                    }

                    // Padding pour alignement sur multiple de 4 avec 0xFF
                    while ((typeBytes.Count + 1) % 4 != 0)
                    {
                        typeBytes.Add(0xFF);
                    }

                    // Écriture finale
                    foreach (var b in typeBytes)
                    {
                        writer.Write(b);
                    }

                    // Valeurs des paramètres
                    if (entry.Variables != null)
                    {
                        foreach (var variable in entry.Variables)
                        {
                            switch (variable.Type)
                            {
                                case CfgValueType.String:
                                    if (variable.Value is string str && !string.IsNullOrEmpty(str) && stringTable.ContainsKey(str))
                                    {
                                        writer.Write(stringTable[str]);
                                    }
                                    else
                                    {
                                        writer.Write(-1); // Chaîne null ou vide
                                    }
                                    break;

                                case CfgValueType.Int:
                                    writer.Write(variable.Value is int intVal ? intVal : 0);
                                    break;

                                case CfgValueType.Float:
                                    writer.Write(variable.Value is float floatVal ? floatVal : 0f);
                                    break;

                                default:
                                    writer.Write(variable.Value is int unknownVal ? unknownVal : 0);
                                    break;
                            }
                        }
                    }
                }

                return stream.ToArray();
            }
        }
    }
}
