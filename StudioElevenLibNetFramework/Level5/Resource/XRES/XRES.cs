using System;
using System.IO;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Resource.RES;

namespace StudioElevenLib.Level5.Resource.XRES
{
    /// <summary>
    /// Represents a Level-5 XRES resource file.
    /// </summary>
    public class XRES : IResource
    {
        /// <summary>
        /// Gets the name of the resource format.
        /// </summary>
        public string Name => "XRES";

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
        public XRES()
        {
            StringTable = new Dictionary<string, uint>();
            Items = new Dictionary<RESType, List<RESElement>>();
        }

        /// <summary>
        /// Reads and decodes an XRES file from a stream.
        /// </summary>
        public XRES(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var reader = new XRESReader(memoryStream.ToArray());
                var result = reader.Read();

                StringTable = result.stringTable;
                Items = result.items;
            }
        }

        /// <summary>
        /// Reads and decodes an XRES file from a byte array.
        /// </summary>
        public XRES(byte[] data)
        {
            var reader = new XRESReader(data);
            var result = reader.Read();

            StringTable = result.stringTable;
            Items = result.items;
        }

        /// <summary>
        /// Reads and decodes an XRES file from an existing BinaryDataReader.
        /// </summary>
        public XRES(BinaryDataReader dataReader)
        {
            var reader = new XRESReader(dataReader);
            var result = reader.Read();

            StringTable = result.stringTable;
            Items = result.items;
        }

        /// <summary>
        /// Encodes and saves the XRES file to a specified file path.
        /// </summary>
        public void Save(string magic, string filepath)
        {
            var writer = new XRESWriter(this);
            writer.Save(magic, filepath);
        }

        /// <summary>
        /// Encodes the XRES file and returns the file bytes.
        /// </summary>
        public byte[] Save(string magic)
        {
            var writer = new XRESWriter(this);
            return writer.Save(magic);
        }
    }
}