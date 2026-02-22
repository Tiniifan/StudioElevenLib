using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.NoCompression;
using System;

namespace StudioElevenLib.Level5.Archive.ARC0
{
    public class ARC0 : IArchive
    {
        public string Name => "ARC0";
        public VirtualDirectory Directory { get; set; }
        public Stream BaseStream { get; private set; }
        public ARC0Support.Header Header { get; private set; }

        public ARC0()
        {
            Directory = new VirtualDirectory("");
        }

        public ARC0(Stream stream)
        {
            BaseStream = stream;
            var reader = new ARC0Reader(stream);
            var result = reader.Read();
            Directory = result.directory;
            Header = result.header;
        }

        public ARC0(byte[] fileByteArray)
        {
            BaseStream = new MemoryStream(fileByteArray);
            var reader = new ARC0Reader(BaseStream);
            var result = reader.Read();
            Directory = result.directory;
            Header = result.header;
        }

        public void Save(string fileName, IProgress<int> progress = null)
        {
            var writer = new ARC0Writer(Directory, Header);
            writer.Save(fileName, progress);
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            var writer = new ARC0Writer(Directory, Header);
            return writer.Save(progress);
        }

        public void Close()
        {
            BaseStream?.Dispose();
            BaseStream = null;
            Directory = null;
        }
    }
}

