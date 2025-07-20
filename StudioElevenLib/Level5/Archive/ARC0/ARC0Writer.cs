﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Compression.NoCompression;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Archive.ARC0
{
    public class ARC0Writer
    {
        private readonly VirtualDirectory _directory;
        private readonly ARC0Support.Header _header;
        private static readonly Encoding ShiftJISEncoding = Encoding.GetEncoding("Shift-JIS");
        private static readonly Encoding UTF8Encoding = Encoding.UTF8;

        public ARC0Writer(VirtualDirectory directory, ARC0Support.Header header)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _header = header;
        }

        public void Save(string fileName, IProgress<int> progress = null)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192))
            {
                WriteToStream(stream, progress);
            }
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                WriteToStream(memoryStream, progress);
                return memoryStream.ToArray();
            }
        }

        private void WriteToStream(Stream stream, IProgress<int> progress = null)
        {
            var writer = new BinaryDataWriter(stream);
            int tableNameOffset = 0;
            int firstFileIndex = 0;
            uint fileOffset = 0;
            var tableName = new List<byte[]>();
            var folders = _directory.GetAllFoldersAsDictionnary();
            var directoryEntries = new List<ARC0Support.DirectoryEntry>(folders.Count);
            var fileEntries = new List<ARC0Support.FileEntry>(folders.Sum(f => f.Value.Files.Count()));
            var files = new Dictionary<ARC0Support.FileEntry, SubMemoryStream>(fileEntries.Capacity);
            var crc32 = new Crc32();

            progress?.Report(0);

            foreach (var folder in folders)
            {
                string directoryName = folder.Key.Length > 1 ? folder.Key.Substring(1) : string.Empty;
                byte[] directoryNameByte = GetShiftJISBytesWithNull(directoryName);
                tableName.Add(directoryNameByte);

                var directoryEntry = new ARC0Support.DirectoryEntry
                {
                    Crc32 = CalculateCrc32(crc32, UTF8Encoding.GetBytes(directoryName)),
                    DirectoryCount = (short)folder.Value.Folders.Count,
                    FirstFileIndex = (ushort)firstFileIndex,
                    FileCount = (short)folder.Value.Files.Count(),
                    DirectoryNameStartOffset = tableNameOffset,
                    FileNameStartOffset = tableNameOffset + directoryNameByte.Length
                };

                if (directoryEntry.Crc32 == 0)
                    directoryEntry.Crc32 = 0xFFFFFFFF;

                directoryEntries.Add(directoryEntry);
                tableNameOffset += directoryNameByte.Length;
                firstFileIndex += folder.Value.Files.Count();

                int nameOffsetInFolder = 0;
                var fileEntryFromFolder = new List<ARC0Support.FileEntry>();
                var filesInFolder = folder.Value.Files.OrderBy(file => file.Key).ToList();

                foreach (var file in filesInFolder)
                {
                    byte[] fileNameByte = GetShiftJISBytesWithNull(file.Key);
                    tableName.Add(fileNameByte);

                    var entryFile = new ARC0Support.FileEntry
                    {
                        Crc32 = CalculateCrc32(crc32, ShiftJISEncoding.GetBytes(file.Key)),
                        NameOffsetInFolder = (uint)nameOffsetInFolder,
                        FileOffset = fileOffset,
                        FileSize = (uint)(file.Value.ByteContent?.Length ?? file.Value.Size)
                    };

                    fileEntryFromFolder.Add(entryFile);
                    files.Add(entryFile, file.Value);

                    tableNameOffset += fileNameByte.Length;
                    nameOffsetInFolder += fileNameByte.Length;
                    fileOffset = (uint)((fileOffset + entryFile.FileSize + 3) & ~3);
                }

                fileEntryFromFolder.Sort((x, y) => x.Crc32.CompareTo(y.Crc32));
                fileEntries.AddRange(fileEntryFromFolder);
            }

            progress?.Report(15);

            var directoryIndex = 0;
            directoryEntries.Sort((x, y) => x.Crc32.CompareTo(y.Crc32));
            for (int i = 0; i < directoryEntries.Count; i++)
            {
                var tempEntry = directoryEntries[i];
                tempEntry.FirstDirectoryIndex = (ushort)directoryIndex;
                directoryIndex += tempEntry.DirectoryCount;
                directoryEntries[i] = tempEntry;
            }

            var directoryHashes = directoryEntries.Select(e => e.Crc32).ToArray();

            progress?.Report(30);

            writer.Seek(0x48);
            long directoryEntriesOffset = 0x48;
            writer.Write(CompressBlockTo(directoryEntries.ToArray(), new NoCompression()));
            writer.WriteAlignment(4);
            progress?.Report(35);

            long directoryHashOffset = stream.Position;
            writer.Write(CompressBlockTo(directoryHashes, new NoCompression()));
            writer.WriteAlignment(4);
            progress?.Report(40);

            long fileEntriesOffset = stream.Position;
            writer.Write(CompressBlockTo(fileEntries.ToArray(), new NoCompression()));
            writer.WriteAlignment(4);
            progress?.Report(45);

            long fileNameTableOffset = stream.Position;
            byte[] tableNameArray = ConcatenateByteArrays(tableName);
            writer.Write(CompressBlockTo(tableNameArray, new NoCompression()));
            writer.WriteAlignment(4);
            progress?.Report(50);

            long dataOffset = stream.Position;
            long totalBytes = files.Sum(file => file.Value.Size);
            long bytesWritten = 0;

            foreach (var file in fileEntries)
            {
                writer.BaseStream.Position = dataOffset + file.FileOffset;
                files[file].CopyTo(stream);
                bytesWritten += file.FileSize;

                if (progress != null && totalBytes > 0)
                {
                    int fileProgress = (int)((double)bytesWritten / totalBytes * 40);
                    progress.Report(50 + fileProgress);
                }
            }

            progress?.Report(90);

            var header = _header;
            header.Magic = 0x30435241;
            header.DirectoryEntriesOffset = (int)directoryEntriesOffset;
            header.DirectoryHashOffset = (int)directoryHashOffset;
            header.FileEntriesOffset = (int)fileEntriesOffset;
            header.NameOffset = (int)fileNameTableOffset;
            header.DataOffset = (int)dataOffset;
            header.DirectoryEntriesCount = (short)directoryEntries.Count;
            header.DirectoryHashCount = (short)directoryHashes.Length;
            header.FileEntriesCount = fileEntries.Count;
            header.DirectoryCount = directoryEntries.Count;
            header.FileCount = fileEntries.Count;
            header.TableChunkSize = ((directoryEntries.Count * 20 +
                                      directoryHashes.Length * 4 +
                                      fileEntries.Count * 16 +
                                      tableNameArray.Length + 0x20 + 3) & ~3);

            writer.Seek(0);
            writer.WriteStruct(header);

            progress?.Report(100);
        }

        private byte[] CompressBlockTo<T>(T[] data, ICompression compression)
        {
            byte[] serializedData = SerializeData(data);
            return compression.Compress(serializedData);
        }

        private byte[] SerializeData<T>(T[] data)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryDataWriter(stream))
                {
                    writer.WriteMultipleStruct<T>(data);
                }
                return stream.ToArray();
            }
        }

        private byte[] GetShiftJISBytesWithNull(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new byte[] { 0 };

            var bytes = ShiftJISEncoding.GetBytes(text);
            var result = new byte[bytes.Length + 1];
            Array.Copy(bytes, result, bytes.Length);
            result[bytes.Length] = 0;
            return result;
        }

        private uint CalculateCrc32(Crc32 crc32, byte[] data)
        {
            var hash = crc32.ComputeHash(data);
            return BitConverter.ToUInt32(hash.Reverse().ToArray(), 0);
        }

        private byte[] ConcatenateByteArrays(List<byte[]> arrays)
        {
            int totalLength = arrays.Sum(arr => arr.Length);
            var result = new byte[totalLength];
            int offset = 0;
            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }
            return result;
        }
    }
}