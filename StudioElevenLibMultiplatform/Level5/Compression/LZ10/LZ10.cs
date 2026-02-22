using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// LZ10 Compression By IcySon55 & onepiecefreak3 : Kuriimu Project - https://github.com/IcySon55/Kuriimu

namespace StudioElevenLib.Level5.Compression.LZ10
{
    public class LZ10 : ICompression
    {
        public byte[] Decompress(byte[] data)
        {
            var p = 4;
            var op = 0;
            var mask = 0;
            var flag = 0;

            List<byte> output = new List<byte>();

            while (p < data.Length)
            {
                if (mask == 0)
                {
                    flag = data[p];
                    p += 1;
                    mask = 0x80;
                }

                if ((flag & mask) == 0)
                {
                    if (p + 1 > data.Length)
                    {
                        break;
                    }
                    output.Add(data[p]);
                    p += 1;
                    op += 1;
                }
                else
                {
                    if (p + 2 > data.Length)
                    {
                        break;
                    }

                    var dat = data[p] << 8 | data[p + 1];
                    p += 2;
                    var pos = (dat & 0x0FFF) + 1;
                    var length = (dat >> 12) + 3;

                    foreach (var i in Enumerable.Range(0, length))
                    {
                        if (op - pos >= 0)
                        {
                            output.Add((byte)(op - pos < output.Count ? output[op - pos] : 0));
                            op += 1;
                        }
                    }
                }

                mask >>= 1;
            }

            return output.ToArray();
        }

        public unsafe byte[] Comp(byte[] indata)
        {
            // make sure the decompressed size fits in 3 bytes.
            // There should be room for four bytes, however I'm not 100% sure if that can be used
            // in every game, as it may not be a built-in function.
            Stream outstream = new MemoryStream();

            // save the input data in an array to prevent having to go back and forth in a file

            int compressedLength = 0;
            fixed (byte* instart = &indata[0])
            {
                // we do need to buffer the output, as the first byte indicates which blocks are compressed.
                // this version does not use a look-ahead, so we do not need to buffer more than 8 blocks at a time.
                byte[] outbuffer = new byte[8 * 2 + 1];
                outbuffer[0] = 0;
                int bufferlength = 1, bufferedBlocks = 0;
                int readBytes = 0;
                while (readBytes < indata.Length)
                {
                    #region If 8 blocks are bufferd, write them and reset the buffer
                    // we can only buffer 8 blocks at a time.
                    if (bufferedBlocks == 8)
                    {
                        outstream.Write(outbuffer, 0, bufferlength);
                        compressedLength += bufferlength;
                        // reset the buffer
                        outbuffer[0] = 0;
                        bufferlength = 1;
                        bufferedBlocks = 0;
                    }
                    #endregion

                    // determine if we're dealing with a compressed or raw block.
                    // it is a compressed block when the next 3 or more bytes can be copied from
                    // somewhere in the set of already compressed bytes.
                    int disp;
                    int oldLength = Math.Min(readBytes, 0x1000);
                    int length = GetOccurrenceLength(instart + readBytes, (int)Math.Min(indata.Length - readBytes, 0x12),
                                                          instart + readBytes - oldLength, oldLength, out disp);


                    // length not 3 or more? next byte is raw data
                    if (length < 3)
                    {
                        outbuffer[bufferlength++] = *(instart + (readBytes++));
                    }
                    else
                    {
                        // 3 or more bytes can be copied? next (length) bytes will be compressed into 2 bytes
                        readBytes += length;

                        // mark the next block as compressed
                        outbuffer[0] |= (byte)(1 << (7 - bufferedBlocks));

                        outbuffer[bufferlength] = (byte)(((length - 3) << 4) & 0xF0);
                        outbuffer[bufferlength] |= (byte)(((disp - 1) >> 8) & 0x0F);
                        bufferlength++;
                        outbuffer[bufferlength] = (byte)((disp - 1) & 0xFF);
                        bufferlength++;
                    }

                    bufferedBlocks++;
                }

                // copy the remaining blocks to the output
                if (bufferedBlocks > 0)
                {
                    outstream.Write(outbuffer, 0, bufferlength);
                    compressedLength += bufferlength;
                }
            }

            outstream.Position = 0;
            return new BinaryReader(outstream).ReadBytes((int)outstream.Length);
        }

        public unsafe byte[] Compress(byte[] indata)
        {
            if (indata.Length > 0x1fffffff)
                throw new Exception("File is too big to be compressed with Level5 compressions!");
            uint methodSize = (uint)indata.Length << 3;
            methodSize |= 0x1;
            using (var bw = new BinaryWriter(new MemoryStream()))
            {
                Stream stream = new MemoryStream(indata.Length);

                bw.Write(methodSize);
                stream.Position = 0;
                var comp = Comp(indata);
                bw.Write(comp);
                bw.BaseStream.Position = 0;
                byte[] compressed = new BinaryReader(bw.BaseStream).ReadBytes((int)bw.BaseStream.Length);
                byte[] compressed1 = new byte[compressed.Length];
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    binaryReader.Read(compressed1, 0, compressed.Length);
                    binaryReader.Close();
                }
                return compressed;
            }
        }

        public unsafe int GetOccurrenceLength(byte* newPtr, int newLength, byte* oldPtr, int oldLength, out int disp, int minDisp = 1)
        {
            disp = 0;
            if (newLength == 0)
                return 0;
            int maxLength = 0;
            // try every possible 'disp' value (disp = oldLength - i)
            for (int i = 0; i < oldLength - minDisp; i++)
            {
                // work from the start of the old data to the end, to mimic the original implementation's behaviour
                // (and going from start to end or from end to start does not influence the compression ratio anyway)
                byte* currentOldStart = oldPtr + i;
                int currentLength = 0;
                // determine the length we can copy if we go back (oldLength - i) bytes
                // always check the next 'newLength' bytes, and not just the available 'old' bytes,
                // as the copied data can also originate from what we're currently trying to compress.
                for (int j = 0; j < newLength; j++)
                {
                    // stop when the bytes are no longer the same
                    if (*(currentOldStart + j) != *(newPtr + j))
                    {
                        break;
                    }
                    currentLength++;
                }

                // update the optimal value
                if (currentLength > maxLength)
                {
                    maxLength = currentLength;
                    disp = oldLength - i;

                    // if we cannot do better anyway, stop trying.
                    if (maxLength == newLength)
                        break;
                }
            }

            return maxLength;
        }
    }
}