using System;
using System.IO;
using System.Text;

namespace VGAudio.Utilities
{
    internal class BinaryReaderBE : BinaryReader
    {
        public BinaryReaderBE(Stream input) : base(input) { }

#if !NET40
        public BinaryReaderBE(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }
#endif

        public override short ReadInt16()
        {
            byte[] buffer = BitConverter.GetBytes(base.ReadInt16());
            return (short)(buffer[0] << 8 | buffer[1]);
        }
        public override ushort ReadUInt16()
        {
            byte[] buffer = BitConverter.GetBytes(base.ReadUInt16());
            return (ushort)(buffer[0] << 8 | buffer[1]);
        }

        public override int ReadInt32()
        {
            byte[] buffer = BitConverter.GetBytes(base.ReadInt32());
            return buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
        }
    }
}