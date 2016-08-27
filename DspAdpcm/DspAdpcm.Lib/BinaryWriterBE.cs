using System.IO;

namespace DspAdpcm.Lib
{
    internal class BinaryWriterBE : BinaryWriter
    {
        public BinaryWriterBE(Stream input) : base(input)
        {
            _buffer = new byte[16];
        }

        private byte[] _buffer;

        public override void Write(short value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(ushort value)
        {
            _buffer[0] = (byte)(value >> 8);
            _buffer[1] = (byte)value;
            OutStream.Write(_buffer, 0, 2);
        }

        public override void Write(int value)
        {
            _buffer[0] = (byte)(value >> 24);
            _buffer[1] = (byte)(value >> 16);
            _buffer[2] = (byte)(value >> 8);
            _buffer[3] = (byte)value;
            OutStream.Write(_buffer, 0, 4);
        }
    }
}
