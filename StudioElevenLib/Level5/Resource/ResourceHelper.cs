using System;
using System.Collections.Generic;
using System.Text;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Resource.Types.Scene3D;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Resource
{
    public static class ResourceHelper
    {
        /// <summary>
        /// Resolves a string name from a CRC32 value using the provided string table.
        /// </summary>
        /// <param name="nameCrc32">The CRC32 hash of the name to resolve.</param>
        /// <param name="stringTable">A dictionary mapping strings to their CRC32 values.</param>
        /// <returns>
        /// The resolved string if a matching CRC32 value is found;
        /// otherwise, a hexadecimal representation of the CRC32 value.
        /// </returns>
        public static string ResolveName(uint nameCrc32, Dictionary<string, uint> stringTable)
        {
            // Search for the name in the string table by comparing CRC32 values
            foreach (var kvp in stringTable)
            {
                if (kvp.Value == (uint)nameCrc32)
                {
                    return kvp.Key;
                }
            }

            // If the name is not found, return its hexadecimal representation
            return "0x" + nameCrc32.ToString("X8");
        }

        public static byte[] ConvertMagicToBytes(string magic)
        {
            byte[] magicBytes = new byte[8];
            byte[] encoded = Encoding.UTF8.GetBytes(magic);

            int length = Math.Min(encoded.Length, 8);
            Array.Copy(encoded, magicBytes, length);

            return magicBytes;
        }

        public static Dictionary<string, (uint crc32, int position)> BuildStringDictionary(Dictionary<RESType, List<RESElement>> items)
        {
            Encoding encoding = Encoding.GetEncoding(932); // Shift-JIS
            Dictionary<string, (uint, int)> stringDict = new Dictionary<string, (uint, int)>();
            int currentPosition = 0;

            foreach (var itemPair in items)
            {
                foreach (var element in itemPair.Value)
                {
                    // Add the name of the element
                    AddStringToDictionary(element.Name, encoding, stringDict, ref currentPosition);

                    // If it is a ResMaterialData, process the images
                    if (element is ResMaterialData materialData)
                    {
                        foreach (var image in materialData.Images)
                        {
                            if (image != null && !string.IsNullOrEmpty(image.Name))
                            {
                                AddStringToDictionary(image.Name, encoding, stringDict, ref currentPosition);
                            }
                        }
                    }
                }
            }

            return stringDict;
        }

        public static void AddStringToDictionary(string name, Encoding encoding, Dictionary<string, (uint, int)> dict, ref int position)
        {
            if (string.IsNullOrEmpty(name) || dict.ContainsKey(name))
                return;

            uint crc32 = Crc32.Compute(encoding.GetBytes(name));
            dict.Add(name, (crc32, position));

            // Position suivante = position actuelle + longueur du string + 1 (pour le 0x00)
            position += encoding.GetByteCount(name) + 1;
        }
    }
}
