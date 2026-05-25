namespace StudioEleven
{
    /// <summary>
    /// Represents a single executable CLI command.
    /// Every command must expose its name, a short description (for global help),
    /// a detailed help text (shown with --help), and an Execute entry point.
    /// </summary>
    public interface ICommand
    {
        /// <summary>The keyword used on the command line (e.g. "decode").</summary>
        string Name { get; }

        /// <summary>One-line description shown in the global help listing.</summary>
        string Description { get; }

        /// <summary>Full usage text shown when the user runs: exe &lt;command&gt; --help</summary>
        string Help { get; }

        /// <summary>
        /// Executes the command.
        /// args[0] is always the command name; extra arguments start at args[1].
        /// Throw an exception on error — Program.cs will catch it and exit(1).
        /// </summary>
        void Execute(string[] args);
    }
}
