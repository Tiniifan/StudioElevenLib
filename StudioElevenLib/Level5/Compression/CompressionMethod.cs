using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Compression
{
    public enum CompressionMethod : uint
    {
        None = 0,
        LZ10 = 1,
        Huffman4 = 2,
        Huffman8 = 3,
        RLE = 4,
        ZLib = 5
    }
}
