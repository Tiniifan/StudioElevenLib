using System;
using System.IO;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Resource.Types;

namespace StudioElevenLib.Level5.Resource.RES
{
    /// <summary>
    /// Represents a Level-5 RES resource file containing nodes, materials, and other 3D scene elements.
    /// </summary>
    public class RES : IResource
    {
        /// <summary>
        /// Gets the name of the resource format.
        /// </summary>
        public string Name => "RES";

        /// <summary>
        /// Dictionary containing the string table hashes and their values.
        /// </summary>
        public Dictionary<string, uint> StringTable { get; set; }

        /// <summary>
        /// Dictionary grouping resource elements by their type.
        /// </summary>
        public Dictionary<RESType, List<RESElement>> Items { get; set; }

        /// <summary>
        /// Empty constructor for manual initialization.
        /// </summary>
        public RES()
        {
            StringTable = new Dictionary<string, uint>();
            Items = new Dictionary<RESType, List<RESElement>>();
        }

        /// <summary>
        /// Reads and decodes a RES file from a stream.
        /// </summary>
        public RES(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var reader = new RESReader(memoryStream.ToArray());
                var result = reader.Read();

                StringTable = result.stringTable;
                Items = result.items;
            }
        }

        /// <summary>
        /// Reads and decodes a RES file from a byte array.
        /// </summary>
        public RES(byte[] data)
        {
            var reader = new RESReader(data);
            var result = reader.Read();

            StringTable = result.stringTable;
            Items = result.items;
        }

        /// <summary>
        /// Reads and decodes a RES file from an existing BinaryDataReader.
        /// </summary>
        public RES(BinaryDataReader dataReader)
        {
            var reader = new RESReader(dataReader);
            var result = reader.Read();

            StringTable = result.stringTable;
            Items = result.items;
        }

        /// <summary>
        /// Encodes and saves the RES file to a specified file path.
        /// </summary>
        public void Save(string magic, string filepath)
        {
            var writer = new RESWriter(this);
            writer.Save(magic, filepath);
        }

        /// <summary>
        /// Encodes the RES file and returns the file bytes.
        /// </summary>
        public byte[] Save(string magic)
        {
            var writer = new RESWriter(this);
            return writer.Save(magic);
        }
    }
}