using System.IO;
using System.Runtime.CompilerServices;

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

        public static byte GetHighNibble(byte value) => (byte)((value >> 4) & 0xF);
        public static byte GetLowNibble(byte value) => (byte)(value & 0xF);

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
                ? GetStream.GetBinaryReader(stream)
                : GetStream.GetBinaryReaderBE(stream);

        public static BinaryWriter GetBinaryWriter(Stream stream, Endianness endianness) =>
            endianness == Endianness.LittleEndian
                ? GetStream.GetBinaryWriter(stream)
                : GetStream.GetBinaryWriterBE(stream);
    }
}
