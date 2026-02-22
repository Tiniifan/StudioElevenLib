using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using StudioElevenLib.Level5.Binary.Logic;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Binary
{
    /// <summary>
    /// Represents a native binary configuration file (.cfgbinnat), providing methods for parsing
    /// and saving structured data using reflection-based type mapping.
    /// Unlike <see cref="CfgBin{TNode}"/>, this format contains no string table, no key table,
    /// and no entry tree — only raw primitive values grouped by type, preceded by a count header.
    /// Supported field types are: byte, sbyte, short, ushort, int, uint, float.
    /// </summary>
    public class CfgBinNat
    {
        /// <summary>
        /// Ordered list of types registered for this file, matching the order they appear in the binary.
        /// </summary>
        private readonly List<Type> _typeOrder;

        /// <summary>
        /// Stores the deserialized instances for each registered type.
        /// </summary>
        private readonly Dictionary<Type, IList> _data;

        /// <summary>
        /// Initializes a new instance of <see cref="CfgBinNat"/> with the given type sequence.
        /// The order of types must match the order in which they appear in the binary file.
        /// </summary>
        /// <param name="types">The ordered collection of types to register.</param>
        public CfgBinNat(IEnumerable<Type> types)
        {
            _typeOrder = types.ToList();
            _data = new Dictionary<Type, IList>();

            foreach (var t in _typeOrder)
            {
                var listType = typeof(List<>).MakeGenericType(t);
                _data[t] = (IList)Activator.CreateInstance(listType);
            }
        }

        /// <summary>
        /// Returns the deserialized list of instances for the specified registered type.
        /// </summary>
        /// <typeparam name="T">The registered type to retrieve instances for.</typeparam>
        /// <returns>The list of deserialized instances of type <typeparamref name="T"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the type was not registered in the constructor.</exception>
        public List<T> GetList<T>() where T : class, new()
        {
            if (!_data.TryGetValue(typeof(T), out var list))
                throw new KeyNotFoundException($"Type {typeof(T).Name} is not registered in this CfgBinNat instance.");

            return (List<T>)list;
        }

        /// <summary>
        /// Opens and parses the native binary data from a byte array.
        /// </summary>
        /// <param name="data">The byte array representing the binary file.</param>
        public void Open(byte[] data)
        {
            Open(new MemoryStream(data));
        }

        /// <summary>
        /// Opens and parses the native binary data from a stream.
        /// The header contains one int count per registered type, followed by the raw instances in order.
        /// </summary>
        /// <param name="stream">The stream representing the binary file.</param>
        public void Open(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream))
            {
                // Read the count for each registered type from the header
                var counts = new int[_typeOrder.Count];
                for (int i = 0; i < _typeOrder.Count; i++)
                    counts[i] = reader.ReadValue<int>();

                // Read the raw instances for each registered type
                for (int i = 0; i < _typeOrder.Count; i++)
                {
                    var type = _typeOrder[i];
                    var list = _data[type];
                    var properties = GetOrderedProperties(type);

                    for (int j = 0; j < counts[i]; j++)
                    {
                        var instance = Activator.CreateInstance(type);

                        foreach (var property in properties)
                        {
                            object value = ReadField(reader, property.PropertyType);
                            property.SetValue(instance, value);
                        }

                        list.Add(instance);
                    }
                }
            }
        }

        /// <summary>
        /// Serializes the current data and saves it to the specified file path.
        /// </summary>
        /// <param name="fileName">The output file path.</param>
        public void Save(string fileName)
        {
            File.WriteAllBytes(fileName, Save());
        }

        /// <summary>
        /// Serializes the current data to a byte array.
        /// The output starts with the count header (one int per type), followed by raw field data.
        /// </summary>
        /// <returns>A byte array representing the serialized file.</returns>
        public byte[] Save()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryDataWriter(stream))
                {
                    // Write the count header
                    foreach (var type in _typeOrder)
                        writer.Write(_data[type].Count);

                    // Write raw field data for each registered type
                    foreach (var type in _typeOrder)
                    {
                        var properties = GetOrderedProperties(type);

                        foreach (var item in _data[type])
                        {
                            foreach (var property in properties)
                            {
                                WriteField(writer, property.PropertyType, property.GetValue(item));
                            }
                        }
                    }

                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Returns the public instance properties of a type that should be serialized,
        /// excluding those marked with <see cref="CfgBinIgnoreAttribute"/>.
        /// Properties are ordered by their <see cref="CfgBinNatOrderAttribute"/> value if present,
        /// otherwise by their metadata token to preserve declaration order.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>An ordered array of mappable properties.</returns>
        private static PropertyInfo[] GetOrderedProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite &&
                            !p.GetCustomAttributes(typeof(CfgBinIgnoreAttribute), true).Any())
                .OrderBy(p =>
                {
                    var attr = (CfgBinNatOrderAttribute)p.GetCustomAttributes(typeof(CfgBinNatOrderAttribute), true).FirstOrDefault();
                    return attr != null ? attr.Order : p.MetadataToken;
                })
                .ToArray();
        }

        /// <summary>
        /// Reads a single primitive value from the reader based on the specified field type.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <param name="type">The expected .NET type of the field.</param>
        /// <returns>The read value boxed as an object.</returns>
        /// <exception cref="NotSupportedException">Thrown if the field type is not supported.</exception>
        private static object ReadField(BinaryDataReader reader, Type type)
        {
            if (type == typeof(byte)) return reader.ReadValue<byte>();
            if (type == typeof(sbyte)) return reader.ReadValue<sbyte>();
            if (type == typeof(short)) return reader.ReadValue<short>();
            if (type == typeof(ushort)) return reader.ReadValue<ushort>();
            if (type == typeof(int)) return reader.ReadValue<int>();
            if (type == typeof(uint)) return reader.ReadValue<uint>();
            if (type == typeof(float)) return reader.ReadValue<float>();

            throw new NotSupportedException($"Field type '{type.Name}' is not supported in CfgBinNat.");
        }

        /// <summary>
        /// Writes a single primitive value to the writer based on the specified field type.
        /// </summary>
        /// <param name="writer">The binary writer to write to.</param>
        /// <param name="type">The .NET type of the field.</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="NotSupportedException">Thrown if the field type is not supported.</exception>
        private static void WriteField(BinaryDataWriter writer, Type type, object value)
        {
            if (type == typeof(byte)) { writer.Write((byte)value); return; }
            if (type == typeof(sbyte)) { writer.Write((sbyte)value); return; }
            if (type == typeof(short)) { writer.Write((short)value); return; }
            if (type == typeof(ushort)) { writer.Write((ushort)value); return; }
            if (type == typeof(int)) { writer.Write((int)value); return; }
            if (type == typeof(uint)) { writer.Write((uint)value); return; }
            if (type == typeof(float)) { writer.Write((float)value); return; }

            throw new NotSupportedException($"Field type '{type.Name}' is not supported in CfgBinNat.");
        }
    }
}