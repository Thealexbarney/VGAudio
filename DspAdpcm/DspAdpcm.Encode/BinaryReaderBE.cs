using System;
using System.IO;

namespace DspAdpcm.Encode
{
    internal class BinaryReaderBE : BinaryReader
    {
        public BinaryReaderBE(Stream input) : base(input) { }

        public short ReadInt16BE()
        {
            byte[] buffer = BitConverter.GetBytes(ReadInt16());
            return (short)(buffer[0] << 8 | buffer[1]);
        }

        public int ReadInt32BE()
        {
            byte[] buffer = BitConverter.GetBytes(ReadInt32());
            return buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
        }
    }
}