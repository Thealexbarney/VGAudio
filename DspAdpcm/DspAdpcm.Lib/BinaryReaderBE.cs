using System;
using System.IO;
using System.Linq;

namespace DspAdpcm.Lib
{
    internal class BinaryReaderBE : BinaryReader
    {
        public BinaryReaderBE(Stream input) : base(input) { }

        public short ReadInt16BE()
        {
            byte[] buffer = BitConverter.GetBytes(ReadInt16());
            return (short)(buffer[0] << 8 | buffer[1]);
        }
        public ushort ReadUInt16BE()
        {
            byte[] buffer = BitConverter.GetBytes(ReadUInt16());
            return (ushort)(buffer[0] << 8 | buffer[1]);
        }

        public int ReadInt32BE()
        {
            byte[] buffer = BitConverter.GetBytes(ReadInt32());
            return buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
        }

        public void Expect(params int[] expected)
        {
            long offset = BaseStream.Position;
            int actual = ReadInt32BE();
            if (!expected.Contains(actual))
            {
                throw new InvalidDataException(
                    $"Expected {(expected.Length > 1 ? "one of: " : "")}" +
                    $"{expected.ToDelimitedString()}, but got {actual} at offset 0x{offset:X}");
            }
        }

        public void Expect(params ushort[] expected)
        {
            long offset = BaseStream.Position;
            ushort actual = ReadUInt16BE();
            if (!expected.Contains(actual))
            {
                throw new InvalidDataException(
                    $"Expected {(expected.Length > 1 ? "one of: " : "")}" +
                    $"{expected.ToDelimitedString()}, but got {actual} at offset 0x{offset:X}");
            }
        }

        public void Expect(params byte[] expected)
        {
            long offset = BaseStream.Position;
            byte actual = ReadByte();
            if (!expected.Contains(actual))
            {
                throw new InvalidDataException(
                    $"Expected {(expected.Length > 1 ? "one of: " : "")}" +
                    $"{expected.ToDelimitedString()}, but got {actual} at offset 0x{offset:X}");
            }
        }
    }
}