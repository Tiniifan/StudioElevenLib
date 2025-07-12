using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Level5.Binary.Logic;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Binary
{
    /// <summary>
    /// Represents a configuration binary file (.cfg), providing methods for parsing and saving the data.
    /// </summary>
    public class CfgBin
    {
        /// <summary>
        /// Gets or sets the encoding used for reading string data.
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets the root entry node for entries in the configuration.
        /// </summary>
        public TreeNode<Entry> Entries { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of strings found in the configuration file.
        /// </summary>
        public Dictionary<int, string> Strings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgBin"/> class.
        /// </summary>
        public CfgBin()
        {
            Entries = new TreeNode<Entry>(new Entry("ROOT"));
            Strings = new Dictionary<int, string>();
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
        /// Saves the current configuration to the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to save the configuration to.</param>
        public void Save(string fileName)
        {
            // Implement save functionality here.
        }

        /// <summary>
        /// Serializes the current configuration to a byte array.
        /// </summary>
        /// <returns>A byte array representing the configuration.</returns>
        public byte[] Save()
        {
            return null;
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
                    Logic.Type[] paramTypes = new Logic.Type[paramCount];

                    int paramIndex = 0;

                    for (int j = 0; j < (int)Math.Ceiling((double)paramCount / 4); j++)
                    {
                        byte paramType = reader.ReadValue<byte>();

                        for (int k = 0; k < 4; k++)
                        {
                            if (paramIndex < paramTypes.Length)
                            {
                                int tag = (paramType >> (2 * k)) & 3;

                                switch (tag)
                                {
                                    case 0:
                                        paramTypes[paramIndex] = Logic.Type.String;
                                        break;
                                    case 1:
                                        paramTypes[paramIndex] = Logic.Type.Int;
                                        break;
                                    case 2:
                                        paramTypes[paramIndex] = Logic.Type.Float;
                                        break;
                                    default:
                                        paramTypes[paramIndex] = Logic.Type.Unknown;
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
                        if (paramTypes[j] == Logic.Type.String)
                        {
                            int offset = reader.ReadValue<int>();
                            string text = offset != -1 && Strings.ContainsKey(offset) ? Strings[offset] : null;
                            variables.Add(new Variable(Logic.Type.String, text));
                        }
                        else if (paramTypes[j] == Logic.Type.Int)
                        {
                            variables.Add(new Variable(Logic.Type.Int, reader.ReadValue<int>()));
                        }
                        else if (paramTypes[j] == Logic.Type.Float)
                        {
                            variables.Add(new Variable(Logic.Type.Float, reader.ReadValue<float>()));
                        }
                        else
                        {
                            variables.Add(new Variable(Logic.Type.Unknown, reader.ReadValue<int>()));
                        }
                    }
                    temp.Add(new Entry(name, variables));
                }
            }
            return temp;
        }

        /// <summary>
        /// Processes the parsed entries and arranges them into a tree structure.
        /// </summary>
        /// <param name="entries">The list of parsed entries.</param>
        /// <returns>The root node of the tree structure representing the entries.</returns>
        private TreeNode<Entry> ProcessEntries(List<Entry> entries)
        {
            var root = new TreeNode<Entry>(new Entry("ROOT"));
            var currentNode = root;

            foreach (var entry in entries)
            {
                string entryName = entry.Name;

                if (entryName != null)
                {
                    if (currentNode != null)
                    {
                        if (entryName.EndsWith("_BEGIN"))
                        {
                            // Create a new node for '_BEGIN'
                            var child = new TreeNode<Entry>(entry);
                            currentNode.AddChild(child);
                            currentNode = child; // Move to the child node
                        }
                        else if (entryName.EndsWith("_END"))
                        {
                            if (currentNode.Parent != null)
                            {
                                // Move back to the parent level
                                currentNode = currentNode.Parent;
                            }
                        }
                        else
                        {
                            // Add a node for other elements
                            var child = new TreeNode<Entry>(entry);
                            currentNode.AddChild(child);
                        }
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
    }
}
