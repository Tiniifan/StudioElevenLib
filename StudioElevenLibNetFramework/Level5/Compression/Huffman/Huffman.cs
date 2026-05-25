using System.IO;

namespace StudioElevenLib.Level5.Compression.Huffman
{
    public class Huffman : ICompression
    {
        public int BitDepth;

        public Huffman(int bitDepth)
        {
            BitDepth = bitDepth;
        }

        /// <summary>
        /// Compresses data using Huffman compression.
        /// </summary>
        public byte[] Compress(byte[] data)
        {
            if (data.Length > 0x1FFFFFFF)
                throw new System.Exception("File is too big to be compressed with Level5 compressions!");

            using (MemoryStream output = new MemoryStream())
            {
                uint mode = (uint)(BitDepth == 4 ? (byte)CompressionMethod.Huffman4 : (byte)CompressionMethod.Huffman8);
                var header = new[]
                {
                    (byte)((byte)(data.Length << 3) | mode),
                    (byte)(data.Length >> 5),
                    (byte)(data.Length >> 13),
                    (byte)(data.Length >> 21)
                };

                output.Write(header, 0, 4);

                new HuffmanEncoder(BitDepth).Encode(data, output);

                return output.ToArray();
            }
        }

        /// <summary>
        /// Decompresses data using Huffman compression.
        /// </summary>
        public byte[] Decompress(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            {
                var decoder = new HuffmanDecoder(BitDepth, NibbleOrder.LowNibbleFirst);
                decoder.Decode(input, output);

                return output.ToArray();
            }
        }
    }
}