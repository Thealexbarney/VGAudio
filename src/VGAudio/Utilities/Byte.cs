namespace VGAudio.Utilities
{
    public static class Byte
    {
        public static short ByteSwap(short value) => (short)ByteSwap((ushort)value);
        public static int ByteSwap(int value) => (int)ByteSwap((uint)value);
        public static long ByteSwap(long value) => (long)ByteSwap((ulong)value);

        public static ushort ByteSwap(ushort value)
        {
            return (ushort)((value >> 8) | (value << 8));
        }
        
        public static uint ByteSwap(uint value)
        {
            value = (value >> 16) | (value << 16);
            return ((value & 0xFF00FF00) >> 8) | ((value & 0x00FF00FF) << 8);
        }

        public static ulong ByteSwap(ulong value)
        {
            value = (value >> 32) | (value << 32);
            value = ((value & 0xFFFF0000FFFF0000) >> 16) | ((value & 0x0000FFFF0000FFFF) << 16);
            return ((value & 0xFF00FF00FF00FF00) >> 8) | ((value & 0x00FF00FF00FF00FF) << 8);
        }
    }
}
