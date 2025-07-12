using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Gets or sets a value indicating whether this entry is an end terminator.
        /// </summary>
        public bool EndTerminator { get; set; }

        /// <summary>
        /// Gets or sets the list of variables associated with this entry.
        /// </summary>
        public List<Variable> Variables { get; set; }

        /// <summary>
        /// Gets or sets the list of child entries under this entry.
        /// </summary>
        public List<Entry> Children { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class.
        /// </summary>
        public Entry()
        {
            Variables = new List<Variable>();
            Children = new List<Entry>();
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
            Children = new List<Entry>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class with a specified name, variables, and end terminator flag.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="variables">The list of variables associated with this entry.</param>
        /// <param name="endTerminator">A value indicating whether this entry is an end terminator.</param>
        public Entry(string name, List<Variable> variables, bool endTerminator)
        {
            Name = name;
            Variables = variables ?? new List<Variable>();
            EndTerminator = endTerminator;
            Children = new List<Entry>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class with a specified name.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        public Entry(string name)
        {
            Name = name;
            Variables = new List<Variable>();
            Children = new List<Entry>();
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