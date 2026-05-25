using System.Collections.Generic;

namespace StudioEleven
{
    /// <summary>
    /// Represents a group of related commands (e.g. image processing, audio, etc.).
    /// Program.cs iterates over all registered modules to build the global help
    /// and the command dispatch table.
    /// </summary>
    public interface IModule
    {
        /// <summary>Display name of the module (e.g. "image").</summary>
        string Name { get; }

        /// <summary>One-line description of what the module does.</summary>
        string Description { get; }

        /// <summary>All commands exposed by this module.</summary>
        IReadOnlyList<ICommand> Commands { get; }
    }
}
