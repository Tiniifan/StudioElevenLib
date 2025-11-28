using System.Collections.Generic;

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
    }
}
