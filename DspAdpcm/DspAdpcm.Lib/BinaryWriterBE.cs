using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DspAdpcm.Lib
{
    class BinaryWriterBE : BinaryWriter
    {
        public BinaryWriterBE(Stream input) : base(input)
        {
            _buffer = new byte[16];
        }

        private byte[] _buffer;

        public void WriteBE(short value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public void WriteBE(ushort value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public void WriteBE(int value)
        {
            _buffer[0] = (byte)(value >> 24);
            _buffer[1] = (byte)(value >> 16);
            _buffer[2] = (byte)(value >> 8);
            _buffer[3] = (byte)value;
            OutStream.Write(_buffer, 0, 4);
        }

        public void WriteASCII(string value)
        {
            byte[] text = System.Text.Encoding.ASCII.GetBytes(value);
            Write(text);
        }
    }
}
