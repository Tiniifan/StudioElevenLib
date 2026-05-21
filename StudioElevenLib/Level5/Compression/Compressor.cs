using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Compression
{
    /// <summary>
    /// Provides helper methods for compression and decompression operations.
    /// </summary>
    public static class Compressor
    {
        /// <summary>
        /// Gets the compression implementation associated with the specified method identifier.
        /// </summary>
        /// <param name="method">Compression method identifier.</param>
        /// <returns>An instance of the matching compression implementation.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the compression method is unknown or unsupported.
        /// </exception>
        public static ICompression GetCompression(CompressionMethod method)
        {
            switch (method)
            {
                case CompressionMethod.None:
                    return new NoCompression.NoCompression();

                case CompressionMethod.LZ10:
                    return new LZ10.LZ10();

                case CompressionMethod.Huffman4:
                    return new Huffman.Huffman(4);

                case CompressionMethod.Huffman8:
                    return new Huffman.Huffman(8);

                case CompressionMethod.RLE:
                    return new RLE.RLE();

                case CompressionMethod.ZLib:
                    return new ZLib.Zlib();

                default:
                    throw new NotSupportedException($"Unknown compression method {method}");
            }
        }

        /// <summary>
        /// Compresses the data using all available algorithms in parallel
        /// and returns the smallest resulting output.
        /// </summary>
        public static byte[] Compress(byte[] data, bool acceptZlib = false)
        {
            List<ICompression> methods = new List<ICompression>
            {
                new NoCompression.NoCompression(),
                new LZ10.LZ10(),
                new Huffman.Huffman(4),
                new Huffman.Huffman(8),
                new RLE.RLE(),
            };

            if (acceptZlib)
            {
                methods.Add(new ZLib.Zlib());
            }

            var results = new byte[methods.Count][];

            // Try all compression methods in parallel
            Parallel.For(0, methods.Count, i =>
            {
                try { results[i] = methods[i].Compress(data); }
                catch { results[i] = null; }
            });

            // Return the smallest valid output
            return results
                .Where(r => r != null && r.Length > 0)
                .OrderBy(r => r.Length)
                .First();
        }

        /// <summary>
        /// Compresses data using a specific compression method.
        /// </summary>
        /// <param name="data">Input data to compress.</param>
        /// <param name="method">Compression method identifier.</param>
        /// <returns>The compressed data.</returns>
        public static byte[] Compress(byte[] data, CompressionMethod method)
        {
            // Get the requested compression method
            ICompression compression = GetCompression(method);

            // Compress the data
            return compression.Compress(data);
        }

        /// <summary>
        /// Decompresses the specified byte array using the detected compression method.
        /// </summary>
        /// <param name="data">Compressed input data.</param>
        /// <returns>The decompressed byte array.</returns>
        public static byte[] Decompress(byte[] data)
        {
            // Read the compression header
            var sizeMethodBuffer = data.Take(4).ToArray();

            // Extract the original uncompressed size
            int size = (sizeMethodBuffer[0] >> 3) | (sizeMethodBuffer[1] << 5) |
                                   (sizeMethodBuffer[2] << 13) | (sizeMethodBuffer[3] << 21);

            // Get the compression method from the header
            CompressionMethod methodId = (CompressionMethod)(BitConverter.ToUInt32(sizeMethodBuffer, 0) & 0x7);
            ICompression method = GetCompression(methodId);

            if (method != null)
            {
                // Decompress and trim to the expected size
                return method.Decompress(data).Take(size).ToArray();
            }
            else
            {
                // Return original data if no valid compression method was found
                return data;
            }
        }

        /// <summary>
        /// Reads compressed data from a stream and decompresses it.
        /// </summary>
        /// <param name="inputStream">Input stream containing compressed data.</param>
        /// <returns>The decompressed byte array.</returns>
        public static byte[] Decompress(Stream inputStream)
        {
            // Allocate a buffer for the entire stream content
            byte[] inputData = new byte[inputStream.Length];

            int totalRead = 0;

            // Read the stream until the buffer is completely filled
            while (totalRead < inputData.Length)
            {
                int bytesRead = inputStream.Read(
                    inputData,
                    totalRead,
                    inputData.Length - totalRead);

                // Stop if the end of the stream is reached
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
            }

            // Resize the buffer if fewer bytes were read
            if (totalRead != inputData.Length)
            {
                Array.Resize(ref inputData, totalRead);
            }

            // Decompress the loaded data
            return Decompress(inputData);
        }
    }
}