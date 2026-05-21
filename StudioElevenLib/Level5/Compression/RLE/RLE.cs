using System;
using System.Collections.Generic;
using System.IO;

namespace StudioElevenLib.Level5.Compression.RLE
{
    public class RLE : ICompression
    {
        /// <summary>
        /// Compresses data using RLE compression.
        /// </summary>
        public byte[] Compress(byte[] indata)
        {
            // Check maximum supported file size
            if (indata.Length > 0x1FFFFFFF)
                throw new Exception("File is too big to be compressed with Level5 compressions!");

            using (MemoryStream output = new MemoryStream())
            {
                // Write compression header
                var header = new[]
                {
                    (byte)((byte)(indata.Length << 3) | 4),
                    (byte)(indata.Length >> 5),
                    (byte)(indata.Length >> 13),
                    (byte)(indata.Length >> 21)
                };

                output.Write(header, 0, 4);

                int i = 0;

                while (i < indata.Length)
                {
                    // Calculate current run length
                    int runLen = 1;

                    while (i + runLen < indata.Length &&
                           indata[i + runLen] == indata[i] &&
                           runLen < 0x82)
                    {
                        runLen++;
                    }

                    if (runLen >= 3)
                    {
                        // Write compressed block
                        output.WriteByte((byte)(0x80 | (runLen - 3)));
                        output.WriteByte(indata[i]);

                        i += runLen;
                    }
                    else
                    {
                        // Write raw block
                        int rawStart = i;
                        int rawLen = 0;

                        while (i < indata.Length && rawLen < 0x80)
                        {
                            // Stop if a compressible run is detected
                            int peek = 1;

                            while (i + peek < indata.Length &&
                                   indata[i + peek] == indata[i] &&
                                   peek < 3)
                            {
                                peek++;
                            }

                            if (peek >= 3)
                                break;

                            rawLen++;
                            i++;
                        }

                        // Safety fallback
                        if (rawLen == 0)
                        {
                            rawLen = 1;
                            i++;
                        }

                        output.WriteByte((byte)(rawLen - 1));
                        output.Write(indata, rawStart, rawLen);
                    }
                }

                // Return compressed data
                return output.ToArray();
            }
        }

        /// <summary>
        /// Decompresses data using RLE compression.
        /// </summary>
        public byte[] Decompress(byte[] instream)
        {
            long inLength = instream.Length;
            long ReadBytes = 0;
            int p = 0;

            p++;

            int decompressedSize = (instream[p++] & 0xFF)
                    | ((instream[p++] & 0xFF) << 8)
                    | ((instream[p++] & 0xFF) << 16);
            ReadBytes += 4;
            if (decompressedSize == 0)
            {
                decompressedSize = decompressedSize
                        | ((instream[p++] & 0xFF) << 24);
                ReadBytes += 4;
            }

            List<byte> outstream = new List<byte>();

            while (p < instream.Length)
            {

                int flag = (byte)instream[p++];
                ReadBytes++;

                bool compressed = (flag & 0x80) > 0;
                int length = flag & 0x7F;

                if (compressed)
                    length += 3;
                else
                    length += 1;

                if (compressed)
                {

                    int data = (byte)instream[p++];
                    ReadBytes++;

                    byte bdata = (byte)data;
                    for (int i = 0; i < length; i++)
                    {
                        outstream.Add(bdata);
                    }

                }
                else
                {

                    int tryReadLength = length;
                    if (ReadBytes + length > inLength)
                        tryReadLength = (int)(inLength - ReadBytes);

                    ReadBytes += tryReadLength;

                    for (int i = 0; i < tryReadLength; i++)
                    {
                        outstream.Add((byte)(instream[p++] & 0xFF));
                    }
                }
            }

            if (ReadBytes < inLength)
            {
            }

            return outstream.ToArray();
        }
    }
}