#nullable enable

using System;
using System.IO;
using System.Text;
using System.Numerics;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Armature.Logic;

namespace StudioElevenLib.Level5.Armature
{
    /// <summary>
    /// Represents a Level-5 MBN (Bone/Armature) file.
    /// Acts as a container for parsing and saving MBN data.
    /// </summary>
    public class MBN
    {
        public string Name => "MBN";

        /// <summary>
        /// The main bone data stored in this MBN file.
        /// </summary>
        public Bone? Bone { get; set; }

        /// <summary>Empty constructor for manual initialization.</summary>
        public MBN() { }

        /// <summary>Reads and decodes an MBN from a stream.</summary>
        public MBN(Stream stream)
        {
            using (stream)
            {
                var reader = new MBNReader(stream);
                Bone = reader.Read();
            }
        }

        /// <summary>Reads and decodes an MBN from a byte array.</summary>
        public MBN(byte[] fileByteArray)
        {
            if (fileByteArray == null || fileByteArray.Length == 0)
                return;

            using (var ms = new MemoryStream(fileByteArray))
            {
                var reader = new MBNReader(ms);
                Bone = reader.Read();
            }
        }

        /// <summary>Creates an MBN from an existing Bone object.</summary>
        public MBN(Bone bone)
        {
            Bone = bone ?? throw new ArgumentNullException(nameof(bone));
        }

        /// <summary>Encodes and saves the MBN to a file.</summary>
        public void Save(string fileName)
        {
            if (Bone == null) throw new InvalidOperationException("Bone data is null.");
            var writer = new MBNWriter(this);
            writer.Save(fileName);
        }

        /// <summary>Encodes the MBN and returns the bytes.</summary>
        public byte[] Save()
        {
            if (Bone == null) throw new InvalidOperationException("Bone data is null.");
            var writer = new MBNWriter(this);
            return writer.Save();
        }
    }
}