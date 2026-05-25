#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StudioElevenLib.Level5.Archive;
using StudioElevenLib.Tools;

namespace StudioEleven.Modules
{
    /// <summary>
    /// Groups all CLI commands for Level 5 archive management.
    /// Register this module in Program.cs to expose commands.
    /// </summary>
    public class ArchiveModule : IModule
    {
        public string Name => "archive";
        public string Description => "Level 5 archive (ARC0 / XFSP / XPCK) interactive shell and conversion";

        public IReadOnlyList<ICommand> Commands { get; } = new List<ICommand>
        {
            new ArcOpenCommand(),
            new ArcConvertCommand(),
        };
    }

    /// <summary>
    /// Opens an archive file fully into memory, and enters an interactive 
    /// standard input/output loop. Every stdin line is processed as a command.
    /// Outputs are provided as plain text.
    /// </summary>
    internal sealed class ArcOpenCommand : ICommand
    {
        public string Name => "arc-open";
        public string Description => "Open an archive file and start an interactive REPL session";
        public string Help =>
            "Usage: exe arc-open <file_path>\n" +
            "  Enters an interactive loop on stdin. Outputs are plain text.\n" +
            "  Available commands inside loop:\n" +
            "    help, info, pwd, cd, ls, get, add, mkdir, delete, rename, move, save, close";

        public void Execute(string[] args)
        {
            if (args.Length < 2)
                throw new Exception("Missing <file_path>. Run: exe arc-open --help");

            string originalPath = Path.GetFullPath(args[1]);
            if (!File.Exists(originalPath))
                throw new FileNotFoundException("Archive file not found.", originalPath);

            // Read the entire file into a MemoryStream to prevent file locks.
            // This allows the user to 'save' and overwrite the original file cleanly.
            byte[] fileBytes = File.ReadAllBytes(originalPath);
            var ms = new MemoryStream(fileBytes);

            IArchive archive = Archiver.GetArchive(ms)
                ?? throw new Exception("Unrecognised archive format.");

            string archiveType = archive.Name ?? "UNKNOWN";
            bool canCreateFolders = archiveType is "ARC0" or "XFSA" or "XFSP";
            string archiveFileName = Path.GetFileName(originalPath);

            // State variables
            VirtualDirectory root = archive.Directory;
            string currentPath = "/";

            // Welcome message and command list
            Console.WriteLine($"Successfully opened {archiveType} archive: {originalPath}");
            PrintHelp();

            // Repl loop REPL LOOP
            while (true)
            {
                // Write shell prompt (e.g. "tapur.xc/> ") and flush so external processes (Python) can read it
                Console.Write($"{archiveFileName}{currentPath}> ");
                Console.Out.Flush();

                string? line = Console.ReadLine();
                if (line == null)
                {
                    Console.WriteLine();
                    break; // EOF
                }

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var cmdArgs = ParseArguments(line);
                    string cmd = cmdArgs[0].ToLowerInvariant();

                    switch (cmd)
                    {
                        case "help":
                            PrintHelp();
                            break;

                        case "close":
                            archive.Close();
                            Console.WriteLine("Session closed.");
                            return; // Exits the REPL and process entirely

                        case "info":
                            Console.WriteLine($"Archive Type      : {archiveType}");
                            Console.WriteLine($"Original Path     : {originalPath}");
                            Console.WriteLine($"Can Create Folders: {canCreateFolders}");
                            break;

                        case "pwd":
                            Console.WriteLine(currentPath);
                            break;

                        case "cd":
                            string cdTarget = cmdArgs.Count > 1 ? cmdArgs[1] : "/";
                            string newPath = VirtualPath.GetAbsolutePath(currentPath, cdTarget);
                            VirtualPath.ResolveDir(root, newPath); // Validates existence
                            currentPath = newPath;
                            // Intentionally no output on successful cd, just like cmd/bash
                            break;

                        case "ls":
                            string lsTarget = cmdArgs.Count > 1 ? cmdArgs[1] : ".";
                            string lsAbsPath = VirtualPath.GetAbsolutePath(currentPath, lsTarget);
                            var targetDir = VirtualPath.ResolveDir(root, lsAbsPath);

                            var folders = targetDir.Folders.Select(f => f.Name).OrderBy(n => n).ToList();
                            var files = targetDir.Files.OrderBy(kv => kv.Key).Select(kv => new
                            {
                                name = kv.Key,
                                size = kv.Value.ByteContent is not null ? (long)kv.Value.ByteContent.Length : kv.Value.Size
                            }).ToList();

                            long totalSize = files.Sum(f => f.size);
                            Console.WriteLine($"Directory of {lsAbsPath}");
                            Console.WriteLine();

                            foreach (var f in folders)
                            {
                                Console.WriteLine($"d-----       {"-",-10} {f}");
                            }
                            foreach (var f in files)
                            {
                                Console.WriteLine($"-r----       {f.size,-10} {f.name}");
                            }

                            Console.WriteLine();
                            Console.WriteLine($"  {folders.Count} Dir(s), {files.Count} File(s) - {totalSize} bytes total.");
                            break;

                        case "get":
                            if (cmdArgs.Count < 2) throw new Exception("Usage: get <file_path>");
                            string getAbsPath = VirtualPath.GetAbsolutePath(currentPath, cmdArgs[1]);
                            var (getParent, getFileName) = VirtualPath.ResolveParent(root, getAbsPath);

                            if (!getParent.Files.TryGetValue(getFileName, out var getStream))
                                throw new FileNotFoundException($"File '{getAbsPath}' not found.");

                            if (getStream.ByteContent is null) getStream.Read();
                            // Only output the raw Base64 string for easy capture
                            Console.WriteLine(Convert.ToBase64String(getStream.ByteContent!));
                            break;

                        case "add":
                            if (cmdArgs.Count < 2) throw new Exception("Usage: add <dest_path>");
                            string addAbsPath = VirtualPath.GetAbsolutePath(currentPath, cmdArgs[1]);

                            // Important: For 'add', we immediately read the *next* line from stdin as Base64.
                            // Python script should send the command, then the base64 string on the next line.
                            string b64 = Console.ReadLine() ?? "";
                            byte[] fileData = Convert.FromBase64String(b64.Trim());

                            var (addParent, addFileName) = VirtualPath.ResolveParent(root, addAbsPath);
                            if (string.IsNullOrEmpty(addFileName))
                                throw new Exception("Destination path must include a filename.");

                            addParent.AddFile(addFileName, new SubMemoryStream(fileData));
                            Console.WriteLine($"Successfully added file: {addAbsPath}");
                            break;

                        case "mkdir":
                            if (!canCreateFolders) throw new Exception("Folder creation is not supported for this archive type.");
                            if (cmdArgs.Count < 2) throw new Exception("Usage: mkdir <dir_path>");

                            string mkAbsPath = VirtualPath.GetAbsolutePath(currentPath, cmdArgs[1]);
                            var (mkParent, mkFolderName) = VirtualPath.ResolveParent(root, mkAbsPath);

                            if (string.IsNullOrEmpty(mkFolderName)) throw new Exception("Invalid directory name.");
                            if (mkParent.GetFolder(mkFolderName) != null) throw new Exception("Folder already exists.");

                            mkParent.AddFolder(mkFolderName);
                            Console.WriteLine($"Successfully created directory: {mkAbsPath}");
                            break;

                        case "delete":
                            if (cmdArgs.Count < 2) throw new Exception("Usage: delete <path>");
                            string delAbsPath = VirtualPath.GetAbsolutePath(currentPath, cmdArgs[1]);
                            var (delParent, delName) = VirtualPath.ResolveParent(root, delAbsPath);

                            if (string.IsNullOrEmpty(delName)) throw new Exception("Cannot delete root directory.");

                            if (delParent.Files.Remove(delName))
                            {
                                Console.WriteLine($"Deleted file: {delAbsPath}");
                            }
                            else if (delParent.GetFolder(delName) is { } folderToRemove)
                            {
                                delParent.Folders.Remove(folderToRemove);
                                Console.WriteLine($"Deleted directory: {delAbsPath}");
                            }
                            else
                            {
                                throw new Exception($"'{delAbsPath}' not found.");
                            }
                            break;

                        case "rename":
                            if (cmdArgs.Count < 3) throw new Exception("Usage: rename <path> <new_name>");
                            string renAbsPath = VirtualPath.GetAbsolutePath(currentPath, cmdArgs[1]);
                            string newName = cmdArgs[2].Trim();

                            var (renParent, oldName) = VirtualPath.ResolveParent(root, renAbsPath);
                            if (string.IsNullOrEmpty(oldName)) throw new Exception("Cannot rename root.");

                            if (renParent.Files.ContainsKey(oldName))
                            {
                                if (renParent.Files.ContainsKey(newName)) throw new Exception("File already exists.");
                                var stream = renParent.Files[oldName];
                                renParent.Files.Remove(oldName);
                                renParent.Files[newName] = stream;
                            }
                            else if (renParent.GetFolder(oldName) is { } folderToRen)
                            {
                                if (renParent.GetFolder(newName) != null) throw new Exception("Folder already exists.");
                                folderToRen.Name = newName;
                            }
                            else
                            {
                                throw new Exception($"'{renAbsPath}' not found.");
                            }

                            Console.WriteLine($"Renamed '{oldName}' to '{newName}'");
                            break;

                        case "move":
                            if (cmdArgs.Count < 3) throw new Exception("Usage: move <src_path> <dest_dir>");
                            string mvSrcAbs = VirtualPath.GetAbsolutePath(currentPath, cmdArgs[1]);
                            string mvDestAbs = VirtualPath.GetAbsolutePath(currentPath, cmdArgs[2]);

                            var (mvSrcParent, mvName) = VirtualPath.ResolveParent(root, mvSrcAbs);
                            var mvDestDir = VirtualPath.ResolveDir(root, mvDestAbs);

                            if (string.IsNullOrEmpty(mvName)) throw new Exception("Cannot move root.");

                            if (mvSrcParent.Files.ContainsKey(mvName))
                            {
                                if (mvDestDir.Files.ContainsKey(mvName)) throw new Exception("File already exists at destination.");
                                var stream = mvSrcParent.Files[mvName];
                                mvSrcParent.Files.Remove(mvName);
                                mvDestDir.AddFile(mvName, stream);
                            }
                            else if (mvSrcParent.GetFolder(mvName) is { } folderToMv)
                            {
                                if (mvDestDir.GetFolder(mvName) != null) throw new Exception("Folder already exists at destination.");
                                mvSrcParent.Folders.Remove(folderToMv);
                                mvDestDir.AddFolder(folderToMv);
                            }
                            else
                            {
                                throw new Exception($"'{mvSrcAbs}' not found.");
                            }

                            Console.WriteLine($"Moved '{mvName}' to '{mvDestAbs}'");
                            break;

                        case "save":
                            string outputPath = cmdArgs.Count > 1 ? cmdArgs[1] : originalPath;

                            // 500 MiB guard for in-memory save if no explicit path is given
                            long MaxInMemoryBytes = 500L * 1024 * 1024;
                            long estimatedSize = archive.Directory.GetSize();
                            if (cmdArgs.Count == 1 && estimatedSize > MaxInMemoryBytes)
                                throw new Exception($"Archive size exceeds 500 MiB. Provide an explicit output_path.");

                            string? destDir = Path.GetDirectoryName(outputPath);
                            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                                Directory.CreateDirectory(destDir);

                            archive.Save(outputPath);
                            Console.WriteLine($"Archive successfully saved to: {outputPath}");
                            break;

                        default:
                            throw new Exception($"Unknown REPL command: '{cmd}'. Type 'help' for a list of commands.");
                    }
                }
                catch (Exception ex)
                {
                    // Catch errors without crashing the interactive loop
                    // Output errors clearly in text for Python to process
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Available commands:");
            Console.WriteLine("  help                         Show this help message");
            Console.WriteLine("  info                         Show archive type and capabilities");
            Console.WriteLine("  pwd                          Print current working directory");
            Console.WriteLine("  cd      <path>               Change working directory");
            Console.WriteLine("  ls      [path]               List folder contents");
            Console.WriteLine("  get     <file_path>          Extract a file as Base64 string");
            Console.WriteLine("  add     <dest_path>          Add/replace a file (Reads next line for Base64)");
            Console.WriteLine("  mkdir   <dir_path>           Create a folder (ARC0 / XFSP only)");
            Console.WriteLine("  delete  <path>               Delete a file or folder");
            Console.WriteLine("  rename  <path> <name>        Rename a file or folder");
            Console.WriteLine("  move    <src> <dest_dir>     Move a file or folder");
            Console.WriteLine("  save    [output_path]        Save archive to disk");
            Console.WriteLine("  close                        Close the session and exit");
            Console.WriteLine();
        }

        /// <summary>
        /// A basic argument parser to handle quoted strings in the REPL (e.g. paths with spaces).
        /// </summary>
        private static List<string> ParseArguments(string commandLine)
        {
            var args = new List<string>();
            bool inQuotes = false;
            var currentArg = new StringBuilder();

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentArg.Length > 0)
                    {
                        args.Add(currentArg.ToString());
                        currentArg.Clear();
                    }
                }
                else
                {
                    currentArg.Append(c);
                }
            }
            if (currentArg.Length > 0) args.Add(currentArg.ToString());
            return args;
        }
    }

    /// <summary>
    /// Converts an on-disk folder directly into a Level 5 archive.
    /// Operates independently of the REPL. Outputs plain text.
    /// </summary>
    internal sealed class ArcConvertCommand : ICommand
    {
        public string Name => "arc-convert";
        public string Description => "Convert an on-disk folder to an archive file (.fa or .xc)";
        public string Help =>
            "Usage: exe arc-convert <folder_path> <format> [output_path]\n" +
            "  format       ARC0 | XFSP | XPCK";

        public void Execute(string[] args)
        {
            if (args.Length < 3)
                throw new Exception("Missing arguments. Run: exe arc-convert --help");

            string folderPath = args[1];
            string formatStr = args[2].ToUpperInvariant();

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Source folder '{folderPath}' does not exist.");

            ArchiveType archiveType = formatStr switch
            {
                "ARC0" => ArchiveType.ARC0,
                "XFSP" => ArchiveType.XFSP,
                "XPCK" => ArchiveType.XPCK,
                _ => throw new Exception($"Unknown format '{formatStr}'. Supported values: ARC0, XFSP, XPCK."),
            };

            string defaultExt = archiveType == ArchiveType.XPCK ? ".xc" : ".fa";
            string folderTrimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outputPath = args.Length >= 4 ? args[3] : folderTrimmed + defaultExt;

            string? destDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            IArchive archive = Archiver.CreateArchiveFromDirectory(folderPath, archiveType);
            archive.Save(outputPath);
            archive.Close();

            Console.WriteLine($"Archive successfully created at: {outputPath}");
        }
    }
}