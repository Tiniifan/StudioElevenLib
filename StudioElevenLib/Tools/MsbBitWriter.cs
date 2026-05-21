using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Tools
{
    public class MsbBitWriter
    {
        private readonly Stream _out;
        private int _buf;
        private int _count;

        public MsbBitWriter(Stream output) => _out = output;

        public void WriteBit(bool one)
        {
            _buf = (_buf << 1) | (one ? 1 : 0);
            if (++_count == 8) { _out.WriteByte((byte)_buf); _buf = 0; _count = 0; }
        }

        public void Flush()
        {
            if (_count > 0)
            {
                _buf <<= (8 - _count);
                _out.WriteByte((byte)_buf);
                _buf = 0; _count = 0;
            }
        }
    }
}
