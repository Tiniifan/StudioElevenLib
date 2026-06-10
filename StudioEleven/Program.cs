using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StudioEleven.Modules;

namespace StudioEleven
{
    class Program
    {
        #region Module registry

        private static readonly List<IModule> RegisteredModules = new()
        {
            new ArchiveModule(),
            new ResourceModule(),
            new ImageModule(),
            new MeshModule(),
            new BoneModule(),
        };

        #endregion

        #region Entry point

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Build a "module command" → ICommand dictionary from all registered modules
            var commandMap = BuildCommandMap();

            // No arguments: print global help
            if (args.Length == 0)
            {
                PrintGlobalHelp();
                Environment.Exit(0);
            }

            // Global help flags
            if (args[0] is "help" or "--help" or "-h")
            {
                PrintGlobalHelp();
                Environment.Exit(0);
            }

            // Reconstruct "module command" key from the first two tokens
            if (args.Length < 2)
            {
                Console.Error.WriteLine($"Unknown command: '{args[0]}'");
                Console.Error.WriteLine();
                PrintGlobalHelp();
                Environment.Exit(1);
            }

            string token = $"{args[0]} {args[1]}".ToLower();

            // Per-command help: exe <module> <command> --help
            if (args.Length >= 3 && args[2] is "--help" or "-h")
            {
                if (commandMap.TryGetValue(token, out ICommand? helpCmd))
                    Console.WriteLine(helpCmd.Help);
                else
                    Console.Error.WriteLine($"Unknown command: '{token}'");
                Environment.Exit(0);
            }

            // Unknown command
            if (!commandMap.TryGetValue(token, out ICommand? command))
            {
                Console.Error.WriteLine($"Unknown command: '{token}'");
                Console.Error.WriteLine();
                PrintGlobalHelp();
                Environment.Exit(1);
            }

            // Execute
            try
            {
                command.Execute(args);
            }
            catch (Exception ex)
            {
                // Send the error to stderr so callers (e.g. Python) can raise
                // a clean exception without polluting stdout.
                Console.Error.WriteLine($"Error [{command.Name}]: {ex.Message}");
                Environment.Exit(1);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Iterates all registered modules and builds a case-insensitive
        /// "module command" → ICommand lookup table.
        /// Throws if two modules register the same "module command" key.
        /// </summary>
        private static Dictionary<string, ICommand> BuildCommandMap()
        {
            var map = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

            foreach (IModule module in RegisteredModules)
            {
                foreach (ICommand cmd in module.Commands)
                {
                    string key = $"{module.Name} {cmd.Name}";
                    if (map.ContainsKey(key))
                        throw new InvalidOperationException(
                            $"Duplicate command '{key}' detected while loading modules.");
                    map[key] = cmd;
                }
            }

            return map;
        }

        /// <summary>
        /// Prints a structured overview of every module and its commands,
        /// plus usage instructions for per-command help.
        /// </summary>
        private static void PrintGlobalHelp()
        {
            string fileVersion =
                Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()
                    ?.Version
                ?? "Unknown";

            Console.WriteLine($"StudioEleven v{fileVersion}");
            Console.WriteLine();

            Console.WriteLine("Usage:");
            Console.WriteLine("  exe <module> <command> [options]");
            Console.WriteLine("  exe <module> <command> --help      Show detailed help for a command");
            Console.WriteLine("  exe --help                         Show this help");
            Console.WriteLine();

            foreach (IModule module in RegisteredModules)
            {
                Console.WriteLine($"[{module.Name}]  {module.Description}");

                foreach (ICommand cmd in module.Commands)
                    Console.WriteLine($"  {module.Name} {cmd.Name,-20} {cmd.Description}");

                Console.WriteLine();
            }
        }

        #endregion
    }
}