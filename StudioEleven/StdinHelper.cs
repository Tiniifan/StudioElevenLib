using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioEleven
{
    /// <summary>
    /// Reads all of stdin, trims whitespace, decodes from Base64 and returns
    /// the raw bytes. Throws a descriptive exception on empty input.
    /// </summary>
    internal static class StdinHelper
    {
        // Exposed as a file-scoped static so every command can call it
        // without duplicating the null-check boilerplate.
        public static byte[] ReadBase64()
        {
            string b64 = Console.In.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(b64))
                throw new Exception("No data received on standard input.");
            return Convert.FromBase64String(b64);
        }
    }
}
