using System.Collections.Generic;

namespace StudioElevenLib.Level5.Binary.Logic
{
    /// <summary>
    /// Represents an entry in the configuration, which may contain variables and child entries.
    /// </summary>
    public class Entry
    {
        /// <summary>
        /// Gets or sets the name of the entry.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the list of variables associated with this entry.
        /// </summary>
        public List<Variable> Variables { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class.
        /// </summary>
        public Entry()
        {
            Variables = new List<Variable>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class with a specified name and variables.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="variables">The list of variables associated with this entry.</param>
        public Entry(string name, List<Variable> variables)
        {
            Name = name;
            Variables = variables ?? new List<Variable>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class with a specified name.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        public Entry(string name)
        {
            Name = name;
            Variables = new List<Variable>();
        }

        /// <summary>
        /// Returns a string representation of the entry, which is its name.
        /// </summary>
        /// <returns>A string representing the name of the entry.</returns>
        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }
}