using System;
using System.IO;
using System.Text;
using System.Linq;
using System.IO.Compression;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Archive;
using StudioElevenLib.Level5.Archive.XFSP;
using StudioElevenLib.Level5.Archive.XPCK;
using StudioElevenLib.Level5.Archive.ARC0;

namespace StudioElevenLib.Level5.Archive
{
    /// <summary>
    /// Provides methods to create and read different types of L5 archives,
    /// </summary>
    public static class Archiver
    {
        /// <summary>
        /// Returns an IArchive instance from a byte array.
        /// </summary>
        public static IArchive GetArchive(byte[] data)
        {
            return GetArchive(new MemoryStream(data));
        }

        /// <summary>
        /// Returns an IArchive instance from a stream by detecting the archive type.
        /// </summary>
        public static IArchive GetArchive(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new ArgumentException("The provided stream doesn't support reading.");
            }

            byte[] magicBytes = new byte[4];
            int bytesRead = stream.Read(magicBytes, 0, 4);

            if (bytesRead < 4)
            {
                throw new ArgumentException("The provided stream is too short to verify the magic number.");
            }

            string magic = Encoding.UTF8.GetString(magicBytes);
            if (magic == "ARC0" || magic == "XFSA")
            {
                stream.Seek(0, SeekOrigin.Begin);
                return new ARC0.ARC0(stream);
            }
            else if (magic == "XFSP")
            {
                stream.Seek(0, SeekOrigin.Begin);
                return new XFSP.XFSP(stream);
            }
            else if (magic == "XPCK")
            {
                stream.Seek(0, SeekOrigin.Begin);
                return new XPCK.XPCK(stream);
            }
            else if (magic.StartsWith("PK\x03\x04"))
            {
                stream.Seek(0, SeekOrigin.Begin);
                VirtualDirectory newDirectory = ConvertZipToVirtualDirectory(stream);

                if (newDirectory.Folders.Any())
                {
                    ARC0.ARC0 arc0 = new ARC0.ARC0();
                    arc0.Directory = newDirectory;
                    return arc0;
                }
                else
                {
                    XPCK.XPCK xpck = new XPCK.XPCK();
                    xpck.Directory = newDirectory;
                    return xpck;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a ZIP stream into a VirtualDirectory structure.
        /// </summary>
        private static VirtualDirectory ConvertZipToVirtualDirectory(Stream zipStream)
        {
            VirtualDirectory root = new VirtualDirectory("/");

            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string[] pathParts = entry.FullName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    VirtualDirectory currentDir = root;

                    for (int i = 0; i < pathParts.Length - 1; i++)
                    {
                        string folderName = pathParts[i];
                        var folder = currentDir.GetFolder(folderName);

                        if (folder == null)
                        {
                            folder = new VirtualDirectory(folderName);
                            currentDir.AddFolder(folder);
                        }

                        currentDir = folder;
                    }

                    if (!entry.FullName.EndsWith("/"))
                    {
                        using (MemoryStream fileStream = new MemoryStream())
                        {
                            entry.Open().CopyTo(fileStream);
                            currentDir.AddFile(pathParts.Last(), new SubMemoryStream(fileStream.ToArray()));
                        }
                    }
                }
            }

            return root;
        }

        /// <summary>
        /// Creates an archive from a directory on disk.
        /// </summary>
        /// <param name="directoryPath">The path of the folder to archive</param>
        /// <param name="archiveType">The type of archive to create</param>
        /// <returns>An IArchive instance containing all files from the folder</returns>
        public static IArchive CreateArchiveFromDirectory(string directoryPath, ArchiveType archiveType)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"The folder '{directoryPath}' does not exist.");
            }

            // Build the VirtualDirectory from the file system
            VirtualDirectory virtualDir = BuildVirtualDirectory(directoryPath, archiveType);

            // Create the appropriate archive based on type
            IArchive archive;
            switch (archiveType)
            {
                case ArchiveType.ARC0:
                case ArchiveType.XFSA:
                    archive = new ARC0.ARC0();
                    break;

                case ArchiveType.XFSP:
                    archive = new XFSP.XFSP();
                    break;

                case ArchiveType.XPCK:
                    archive = new XPCK.XPCK();
                    break;

                default:
                    throw new ArgumentException($"Unsupported archive type: {archiveType}");
            }

            archive.Directory = virtualDir;
            return archive;
        }

        /// <summary>
        /// Builds a VirtualDirectory from a file system directory.
        /// </summary>
        private static VirtualDirectory BuildVirtualDirectory(string directoryPath, ArchiveType archiveType)
        {
            VirtualDirectory rootDir = new VirtualDirectory(Path.GetFileName(directoryPath) ?? "");

            // For XFSP and XPCK, only take root files (non-recursive)
            bool isFlat = archiveType == ArchiveType.XFSP || archiveType == ArchiveType.XPCK;

            if (isFlat)
            {
                // Non-recursive: only files in the root
                AddFilesFromDirectory(rootDir, directoryPath);
            }
            else
            {
                // Recursive for ARC0 and XFSA
                AddFilesAndFoldersRecursively(rootDir, directoryPath);
            }

            return rootDir;
        }

        /// <summary>
        /// Adds only the files in a folder (no subfolders).
        /// </summary>
        private static void AddFilesFromDirectory(VirtualDirectory virtualDir, string directoryPath)
        {
            string[] files = Directory.GetFiles(directoryPath);

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                long fileSize = fileStream.Length;
                SubMemoryStream subStream = new SubMemoryStream(fileStream, 0, fileSize);

                virtualDir.AddFile(fileName, subStream);
            }
        }

        /// <summary>
        /// Recursively adds all files and subfolders from a directory.
        /// </summary>
        private static void AddFilesAndFoldersRecursively(VirtualDirectory virtualDir, string directoryPath)
        {
            // Add all files in the current folder
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                byte[] fileData = File.ReadAllBytes(filePath);
                SubMemoryStream subStream = new SubMemoryStream(fileData);

                virtualDir.AddFile(fileName, subStream);
            }

            // Recursively traverse subfolders
            string[] subDirectories = Directory.GetDirectories(directoryPath);
            foreach (string subDirPath in subDirectories)
            {
                string subDirName = Path.GetFileName(subDirPath);
                VirtualDirectory subVirtualDir = new VirtualDirectory(subDirName);

                // Recursive call to fill subfolder
                AddFilesAndFoldersRecursively(subVirtualDir, subDirPath);

                virtualDir.AddFolder(subVirtualDir);
            }
        }
    }
}