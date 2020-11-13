using System;
using VGAudio.Utilities;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaKey
    {
        public CriHcaKey(ulong keyCode)
        {
            KeyCode = keyCode;
            DecryptionTable = CreateDecryptionTable(keyCode);
            EncryptionTable = InvertTable(DecryptionTable);
            KeyType = 56;
        }

        public CriHcaKey(Type type)
        {
            switch (type)
            {
                case Type.Type0:
                    DecryptionTable = CreateDecryptionTableType0();
                    KeyType = 0;
                    break;
                case Type.Type1:
                    DecryptionTable = CreateDecryptionTableType1();
                    KeyType = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            EncryptionTable = InvertTable(DecryptionTable);
        }

        public int KeyType { get; }
        public ulong KeyCode { get; }
        public byte[] DecryptionTable { get; }
        public byte[] EncryptionTable { get; }

        private static byte[][] Rows { get; } = GenerateAllRows();

        private static byte[] CreateDecryptionTable(ulong keyCode)
        {
            byte[] kc = BitConverter.GetBytes(keyCode - 1);
            var seed = new byte[16];

            seed[0] = kc[1];
            seed[1] = (byte)(kc[6] ^ kc[1]);
            seed[2] = (byte)(kc[2] ^ kc[3]);
            seed[3] = kc[2];
            seed[4] = (byte)(kc[1] ^ kc[2]);
            seed[5] = (byte)(kc[3] ^ kc[4]);
            seed[6] = kc[3];
            seed[7] = (byte)(kc[2] ^ kc[3]);
            seed[8] = (byte)(kc[4] ^ kc[5]);
            seed[9] = kc[4];
            seed[10] = (byte)(kc[3] ^ kc[4]);
            seed[11] = (byte)(kc[5] ^ kc[6]);
            seed[12] = kc[5];
            seed[13] = (byte)(kc[4] ^ kc[5]);
            seed[14] = (byte)(kc[6] ^ kc[1]);
            seed[15] = kc[6];

            return CreateTable(kc[0], seed);
        }

        private static byte[] CreateDecryptionTableType0()
        {
            var table = new byte[256];

            for (int i = 0; i < 256; i++)
            {
                table[i] = (byte)i;
            }

            return table;
        }

        private static byte[] CreateDecryptionTableType1()
        {
            var table = new byte[256];
            int xor = 0;
            const int mult = 13;
            const int inc = 11;
            int outPos = 1;

            for (int i = 0; i < 256; i++)
            {
                xor = (xor * mult + inc) % 256;
                if (xor != 0 && xor != 0xff)
                {
                    table[outPos++] = (byte)xor;
                }
            }

            table[0xff] = 0xff;
            return table;
        }

        private static byte[] CreateTable(byte rowSeed, byte[] columnSeeds)
        {
            var table = new byte[256];
            byte[] row = Rows[rowSeed];

            for (int r = 0; r < 16; r++)
            {
                byte[] column = Rows[columnSeeds[r]];
                for (int c = 0; c < 16; c++)
                {
                    table[16 * r + c] = Helpers.CombineNibbles(row[r], column[c]);
                }
            }

            return ShuffleTable(table);
        }

        public static byte[] CreateRandomRow(byte seed)
        {
            var row = new byte[16];
            int xor = seed >> 4;
            int mult = ((seed & 1) << 3) | 5;
            int inc = (seed & 0xe) | 1;

            for (int i = 0; i < 16; i++)
            {
                xor = (xor * mult + inc) % 16;
                row[i] = (byte)xor;
            }

            return row;
        }

        private static byte[][] GenerateAllRows()
        {
            var rows = new byte[0x100][];

            for (int i = 0; i < 0x100; i++)
            {
                rows[i] = CreateRandomRow((byte)i);
            }

            return rows;
        }

        private static byte[] ShuffleTable(byte[] tableIn)
        {
            var table = new byte[256];
            byte x = 0;
            int outPos = 1;

            for (int i = 0; i < 256; i++)
            {
                x += 17;
                if (tableIn[x] != 0 && tableIn[x] != 0xff)
                {
                    table[outPos++] = tableIn[x];
                }
            }

            table[0xff] = 0xff;
            return table;
        }

        public static byte[] InvertTable(byte[] tableIn)
        {
            int length = tableIn.Length;
            var table = new byte[length];

            for (int i = 0; i < length; i++)
            {
                table[tableIn[i]] = (byte)i;
            }

            return table;
        }

        public enum Type
        {
            Type0 = 0,
            Type1 = 1
        }
    }
}
