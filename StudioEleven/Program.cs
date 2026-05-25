using System;
using System.Collections.Generic;
using System.Reflection;
using StudioEleven.Modules;

namespace StudioEleven
{
    class Program
    {
        #region Module registry

        private static readonly List<IModule> RegisteredModules = new()
        {
            new ImageModule(),
        };

        #endregion

        #region Entry point

        static void Main(string[] args)
        {
            // Build a flat name→command dictionary from all registered modules
            var commandMap = BuildCommandMap();

            // No arguments: print global help
            if (args.Length == 0)
            {
                PrintGlobalHelp(commandMap);
                Environment.Exit(0);
            }

            string token = args[0].ToLower();

            // Global help flags
            if (token is "help" or "--help" or "-h")
            {
                PrintGlobalHelp(commandMap);
                Environment.Exit(0);
            }

            // Unknown command
            if (!commandMap.TryGetValue(token, out ICommand? command))
            {
                Console.Error.WriteLine($"Unknown command: '{args[0]}'");
                Console.Error.WriteLine();
                PrintGlobalHelp(commandMap);
                Environment.Exit(1);
            }

            // Per-command help: exe <command> --help
            if (args.Length >= 2 && args[1] is "--help" or "-h")
            {
                Console.WriteLine(command.Help);
                Environment.Exit(0);
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
        /// command-name → ICommand lookup table.
        /// Throws if two modules register the same command name.
        /// </summary>
        private static Dictionary<string, ICommand> BuildCommandMap()
        {
            var map = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

            foreach (IModule module in RegisteredModules)
            {
                foreach (ICommand cmd in module.Commands)
                {
                    if (map.ContainsKey(cmd.Name))
                        throw new InvalidOperationException(
                            $"Duplicate command name '{cmd.Name}' detected while loading modules.");
                    map[cmd.Name] = cmd;
                }
            }

            return map;
        }

        /// <summary>
        /// Prints a structured overview of every module and its commands,
        /// plus usage instructions for per-command help.
        /// </summary>
        private static void PrintGlobalHelp(Dictionary<string, ICommand> commandMap)
        {
            string fileVersion =
                Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()
                    ?.Version
                ?? "Unknown";

            Console.WriteLine($"StudioEleven v{fileVersion}");
            Console.WriteLine();

            Console.WriteLine("Usage:");
            Console.WriteLine("  exe <command> [options]");
            Console.WriteLine("  exe <command> --help      Show detailed help for a command");
            Console.WriteLine("  exe --help                Show this help");
            Console.WriteLine();

            foreach (IModule module in RegisteredModules)
            {
                Console.WriteLine($"[{module.Name}]  {module.Description}");

                foreach (ICommand cmd in module.Commands)
                    Console.WriteLine($"  {cmd.Name,-20} {cmd.Description}");

                Console.WriteLine();
            }
        }

        #endregion
    }
}
