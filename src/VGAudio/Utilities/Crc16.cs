namespace VGAudio.Utilities
{
    public class Crc16
    {
        private ushort[] Table { get; }

        public Crc16(ushort polynomial)
        {
            Table = GenerateTable(polynomial);
        }

        public ushort Compute(byte[] data, int size)
        {
            ushort crc = 0;
            for (int i = 0; i < size; i++)
                crc = (ushort)((crc << 8) ^ Table[(crc >> 8) ^ data[i]]);
            return crc;
        }

        private static ushort[] GenerateTable(ushort polynomial)
        {
            var table = new ushort[256];
            for (int i = 0; i < table.Length; i++)
            {
                ushort curByte = (ushort)(i << 8);

                for (byte j = 0; j < 8; j++)
                {
                    bool xorFlag = (curByte & 0x8000) != 0;
                    curByte <<= 1;
                    if (xorFlag) curByte ^= polynomial;
                }

                table[i] = curByte;
            }

            return table;
        }
    }
}
