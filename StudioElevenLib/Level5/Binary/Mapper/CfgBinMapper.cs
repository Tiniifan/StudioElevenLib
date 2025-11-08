using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Level5.Binary.Logic;

namespace StudioElevenLib.Level5.Binary.Mapper
{
    /// <summary>
    /// Static utility class for converting between TreeNode structures and class instances.
    /// Provides functionality to flatten TreeNode structures into lists of class instances
    /// and rebuild TreeNode structures from class instance lists.
    /// </summary>
    public static class CfgBinMapper
    {
        /// <summary>
        /// Flattens a TreeNode structure into a list of class instances of the specified type.
        /// Only processes nodes that match the specified entry name and ignores properties marked with CfgbinIgnoreAttribute.
        /// </summary>
        /// <typeparam name="T">The type of class to create instances for</typeparam>
        /// <param name="rootNode">The root TreeNode to process</param>
        /// <param name="targetEntryName">The name of entries to process (e.g., "CHARA_PARAM_INFO")</param>
        /// <returns>A list of class instances populated with data from matching TreeNode entries</returns>
        public static List<T> FlattenEntryToClassList<T>(CfgTreeNode rootNode, string targetEntryName) where T : class, new()
        {
            if (rootNode == null)
                throw new ArgumentNullException(nameof(rootNode));

            if (string.IsNullOrEmpty(targetEntryName))
                throw new ArgumentException("Target entry name cannot be null or empty", nameof(targetEntryName));

            var result = new List<T>();
            var targetNodes = new List<CfgTreeNode>();

            // Find all nodes with the target entry name
            FindTargetNodes(rootNode, targetEntryName, targetNodes);

            // Get properties that should be mapped (excluding ignored ones)
            var targetType = typeof(T);
            var properties = GetMappableProperties(targetType);

            // Convert each target node to a class instance
            foreach (var node in targetNodes)
            {
                var instance = CreateInstanceFromNode<T>(node, properties);
                result.Add(instance);
            }

            return result;
        }

        /// <summary>
        /// Rebuilds a TreeNode structure from a list of class instances.
        /// Creates a root node with the specified parent entry name and adds child nodes for each class instance.
        /// </summary>
        /// <typeparam name="T">The type of class instances in the input list</typeparam>
        /// <param name="instances">The list of class instances to convert</param>
        /// <param name="parentEntryName">The name for the root entry (e.g., "CHARA_PARAM_INFO_BEGIN")</param>
        /// <param name="childEntryName">The name for child entries (e.g., "CHARA_PARAM_INFO")</param>
        /// <returns>A TreeNode structure representing the class instances</returns>
        public static CfgTreeNode BuildEntryFromClassList<T>(List<T> instances, string parentEntryName, string childEntryName) where T : class
        {
            if (instances == null)
                throw new ArgumentNullException(nameof(instances));

            if (string.IsNullOrEmpty(parentEntryName))
                throw new ArgumentException("Parent entry name cannot be null or empty", nameof(parentEntryName));

            if (string.IsNullOrEmpty(childEntryName))
                throw new ArgumentException("Child entry name cannot be null or empty", nameof(childEntryName));

            // Create the root node
            var rootEntry = new Entry(parentEntryName);
            var rootNode = new CfgTreeNode(rootEntry, 1);

            // Get properties that should be mapped (excluding ignored ones)
            var targetType = typeof(T);
            var properties = GetMappableProperties(targetType);

            // Create child nodes for each instance
            foreach (var instance in instances)
            {
                var childNode = CreateNodeFromInstance(instance, childEntryName, properties, 2);
                rootNode.AddChild(childNode);
            }

            return rootNode;
        }

        /// <summary>
        /// Recursively finds all nodes with the specified entry name.
        /// </summary>
        /// <param name="currentNode">The current node being examined</param>
        /// <param name="targetEntryName">The entry name to search for</param>
        /// <param name="foundNodes">The list to store found nodes</param>
        private static void FindTargetNodes(CfgTreeNode currentNode, string targetEntryName, List<CfgTreeNode> foundNodes)
        {
            // Check if current node matches the target
            if (currentNode.Item?.Name == targetEntryName)
            {
                foundNodes.Add(currentNode);
            }

            // Recursively search children
            foreach (var child in currentNode.Children)
            {
                if (child is CfgTreeNode cfgChild)
                    FindTargetNodes(cfgChild, targetEntryName, foundNodes);
            }
        }

        /// <summary>
        /// Gets all properties of a type that should be mapped, excluding those marked with CfgbinIgnoreAttribute.
        /// Properties are returned in declaration order.
        /// </summary>
        /// <param name="type">The type to examine</param>
        /// <returns>An array of PropertyInfo objects for mappable properties</returns>
        private static PropertyInfo[] GetMappableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                      .Where(prop => prop.CanRead && prop.CanWrite &&
                            !prop.GetCustomAttributes(typeof(CfgBinIgnoreAttribute), true).Any())
                      .ToArray();
        }

        /// <summary>
        /// Creates a class instance from a TreeNode entry, mapping variables to properties.
        /// </summary>
        /// <typeparam name="T">The type of class to create</typeparam>
        /// <param name="node">The TreeNode containing the data</param>
        /// <param name="properties">The properties to map to</param>
        /// <returns>A new instance of type T populated with data from the node</returns>
        private static T CreateInstanceFromNode<T>(CfgTreeNode node, PropertyInfo[] properties) where T : class, new()
        {
            var instance = new T();
            var variables = node.Item?.Variables ?? new List<Variable>();

            // Map variables to properties by index
            for (int i = 0; i < Math.Min(variables.Count, properties.Length); i++)
            {
                var variable = variables[i];
                var property = properties[i];

                try
                {
                    // Convert and set the property value
                    var convertedValue = ConvertValue(variable.Value, property.PropertyType);
                    property.SetValue(instance, convertedValue);
                }
                catch (Exception ex)
                {
                    // Log or handle conversion errors as needed
                    throw new InvalidOperationException(
                        $"Failed to convert value '{variable.Value}' to type '{property.PropertyType.Name}' for property '{property.Name}'", ex);
                }
            }

            return instance;
        }

        /// <summary>
        /// Creates a TreeNode from a class instance, converting properties to variables.
        /// </summary>
        /// <typeparam name="T">The type of the class instance</typeparam>
        /// <param name="instance">The class instance to convert</param>
        /// <param name="entryName">The name for the entry</param>
        /// <param name="properties">The properties to map from</param>
        /// <param name="level">The level for the TreeNode</param>
        /// <returns>A new TreeNode representing the class instance</returns>
        private static CfgTreeNode CreateNodeFromInstance<T>(T instance, string entryName, PropertyInfo[] properties, int level) where T : class
        {
            var variables = new List<Variable>();

            // Convert each property to a variable
            foreach (var property in properties)
            {
                CfgValueType cfgType;

                var value = property.GetValue(instance);

                // If the property type is object, we try to guess from the value
                if (property.PropertyType == typeof(object))
                {
                    if (value != null)
                        cfgType = GetVariableType(value.GetType());
                    else
                        cfgType = CfgValueType.Int; // default value if null
                }
                else
                {
                    cfgType = GetVariableType(property.PropertyType);
                }

                var variable = new Variable
                {
                    Name = property.Name,
                    Value = value,
                    Type = cfgType
                };

                variables.Add(variable);
            }

            var entry = new Entry(entryName, variables);
            return new CfgTreeNode(entry, level);
        }

        /// <summary>
        /// Converts a value to the specified target type, handling type coercion as needed.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">The target type to convert to</param>
        /// <returns>The converted value</returns>
        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return GetDefaultValue(targetType);

            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            // If types match, return as-is
            if (value.GetType() == targetType)
                return value;

            // Use TypeConverter for conversion
            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(value.GetType()))
            {
                return converter.ConvertFrom(value);
            }

            // Fallback to Convert.ChangeType
            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Gets the default value for a type.
        /// </summary>
        /// <param name="type">The type to get the default value for</param>
        /// <returns>The default value for the specified type</returns>
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// Maps a .NET type to a Variable type enum.
        /// </summary>
        /// <param name="propertyType">The .NET type to map</param>
        /// <returns>The corresponding Variable type</returns>
        private static CfgValueType GetVariableType(System.Type propertyType)
        {
            // Handle nullable types
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propertyType = Nullable.GetUnderlyingType(propertyType);
            }

            if (propertyType == typeof(int) || propertyType == typeof(long) ||
                propertyType == typeof(short) || propertyType == typeof(byte) ||
                propertyType == typeof(uint) || propertyType == typeof(ulong) ||
                propertyType == typeof(ushort) || propertyType == typeof(sbyte))
            {
                return CfgValueType.Int;
            }

            if (propertyType == typeof(float) || propertyType == typeof(double) || propertyType == typeof(decimal))
            {
                return CfgValueType.Float;
            }

            if (propertyType == typeof(string))
            {
                return CfgValueType.String;
            }

            return CfgValueType.Unknown;
        }
    }
}
