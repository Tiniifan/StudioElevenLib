using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Tools
{
    public class MsbBitWriter : IDisposable
    {
        private BinaryWriter _writer;
        private uint _buffer = 0;
        private int _bitCount = 0;

        public MsbBitWriter(Stream outputStream)
        {
            _writer = new BinaryWriter(outputStream, System.Text.Encoding.Default, leaveOpen: true);
        }

        public void WriteBit(bool bit)
        {
            if (bit)
            {
                _buffer |= (1u << (31 - _bitCount));
            }

            _bitCount++;

            if (_bitCount == 32)
            {
                FlushBuffer();
            }
        }

        public void Flush()
        {
            if (_bitCount > 0)
            {
                FlushBuffer();
            }
        }

        private void FlushBuffer()
        {
            _writer.Write(_buffer);
            _buffer = 0;
            _bitCount = 0;
        }

        public void Dispose()
        {
            Flush();
            _writer?.Dispose();
        }
    }
}