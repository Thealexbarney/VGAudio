using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace VGAudio.Utilities
{
    public static class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Clamp16(int value)
        {
            if (value > short.MaxValue)
                return short.MaxValue;
            if (value < short.MinValue)
                return short.MinValue;
            return (short)value;
        }

        private static sbyte[] _signedNibbles = { 0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1 };

        public static byte GetHighNibble(byte value) => (byte)((value >> 4) & 0xF);
        public static byte GetLowNibble(byte value) => (byte)(value & 0xF);

        public static sbyte GetHighNibbleSigned(byte value) => _signedNibbles[(value >> 4) & 0xF];
        public static sbyte GetLowNibbleSigned(byte value) => _signedNibbles[value & 0xF];

        public static int GetNextMultiple(int value, int multiple)
        {
            if (multiple <= 0)
                return value;

            if (value % multiple == 0)
                return value;

            return value + multiple - value % multiple;
        }


        public static bool LoopPointsAreAligned(int loopStart, int alignmentMultiple)
            => !(alignmentMultiple != 0 && loopStart % alignmentMultiple != 0);

        public static BinaryReader GetBinaryReader(Stream stream, Endianness endianness) =>
            endianness == Endianness.LittleEndian
                ? new BinaryReader(stream, Encoding.UTF8, true)
                : new BinaryReaderBE(stream, Encoding.UTF8, true);

        public static BinaryWriter GetBinaryWriter(Stream stream, Endianness endianness) =>
            endianness == Endianness.LittleEndian
                ? new BinaryWriter(stream, Encoding.UTF8, true)
                : new BinaryWriterBE(stream, Encoding.UTF8, true);
    }
}
