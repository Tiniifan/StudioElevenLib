using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Binary.Logic
{
    /// <summary>
    /// Represents a variable with a specific type, value, and name.
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// Gets or sets the type of the variable.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the value of the variable.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the name of the variable.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class.
        /// </summary>
        public Variable()
        {
            // Default constructor
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class with a specified type and value.
        /// </summary>
        /// <param name="type">The type of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        public Variable(Type type, object value)
        {
            Type = type;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class by copying another variable.
        /// </summary>
        /// <param name="variable">The variable to copy.</param>
        public Variable(Variable variable)
        {
            Type = variable.Type;
            Value = variable.Value;
            Name = variable.Name;
        }
    }
}

