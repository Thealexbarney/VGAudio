using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VGAudio.Codecs.CriHca;
using VGAudio.Utilities;

namespace VGAudio.Tools.CrackHca
{
    internal class Solver
    {
        public byte[][] SubTables { get; } = GenerateSubTables();
        public byte[][][] BackwardShuffle { get; } = GenerateShufflers(backward: true);
        public byte[][][] ForwardShuffle { get; } = GenerateShufflers(backward: false);

        public Crack.Frequency[][] Frequencies { get; }
        public List<byte> Byte2Zeros { get; } = new List<byte>(0x100);
        public List<ShufflerId> PossibleShufflers { get; set; } = new List<ShufflerId>();
        public int UpperSeed { get; set; }

        public Solver(Crack.Frequency[][] frequencies)
        {
            Frequencies = frequencies;

            for (int i = 0; i < 0x100; i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (frequencies[2][i].Freq == 0)
                {
                    Byte2Zeros.Add(frequencies[2][i].Value);
                }
            }
        }

        public void Solve()
        {
            FilterPossibleShufflersWithByte2Zeros();
            FindUpperSeed();
            if (UpperSeed < 0) return;
            FindShuffleTable();

            var table = new Table(PossibleShufflers[0], UpperSeed);
            table.AddKnownValue(Frequencies[4][0].Value, 0x7d);
            table.RunCycles();
        }

        /// <summary>
        /// Finds possible tables used to shuffle the decryption table without using a known upper seed
        /// </summary>
        /// <remarks>
        /// The third byte in a frame contains the first 8 bits of the 9-bit noise level value.
        /// The noise level is usually well below 200, which means the high bit of the third byte is always 0.
        /// To exploit this, we go through each possible shuffle table to find one that results in an unshuffled
        /// table where 9 rows contain only values that are never used in byte 3 of any frame.
        /// </remarks>
        public void FilterPossibleShufflersWithByte2Zeros()
        {
            int[] count = new int[16];
            for (int pos1 = 0; pos1 < 0x100; pos1++)
            {
                for (int pos2 = pos1; pos2 < 0x100; pos2++)
                {
                    Array.Clear(count, 0, 16);
                    foreach (byte v in Byte2Zeros)
                    {
                        count[BackwardShuffle[pos1][pos2][v] >> 4]++;
                    }
                    int emptyRows = count.Count(c => c >= 15);
                    if (emptyRows >= 9)
                    {
                        PossibleShufflers.Add(new ShufflerId(pos1, pos2));
                    }
                }
            }
        }

        /// <summary>
        /// Finds possible upper seeds for the encryption table.
        /// </summary>
        /// <remarks>
        /// The frequency of values of the third byte of each frame follows a pattern.
        /// The upper nibble of the most frequent 16 values is usually 5. For the next 16 values, it's usually 4, then 3, then 2.
        /// These will be mixed up a bit, but there will usually be a dominant value in each grouping of 16 bytes.
        /// Knowing 4 values of a sub-table is enough to determine the seed.
        /// </remarks>
        public void FindUpperSeed()
        {
            var counts = Helpers.CreateJaggedArray<int[][]>(4, 16);
            // Row indexes of the rows with the upper nibble values 5, 4, 3, and 2, respectively
            var rowIndex = new int[4];

            // Determine row indexes from frequency analysis
            foreach (var table in PossibleShufflers)
            {
                int position = 0;
                foreach (int[] count in counts)
                {
                    byte[] shuffler = BackwardShuffle[table.Pos1][table.Pos2];
                    for (int i = 0; i < 16; i++)
                    {
                        count[shuffler[Frequencies[2][position++].Value] >> 4]++;
                    }
                }
            }
            for (int i = 0; i < counts.Length; i++)
            {
                rowIndex[i] = counts[i].ToList().IndexOf(counts[i].Max());
            }

            // Find the seed from the row indexes
            int[] possibleSeeds = PossibleSeeds(rowIndex);

            UpperSeed = possibleSeeds.Length > 0 ? possibleSeeds[0] : -1;
        }

        private int[] PossibleSeeds(int[] rowIndex)
        {
            var possibleSubTables = SubTables.AsEnumerable();

            for (int i = 0; i < rowIndex.Length; i++)
            {
                int iLocal = i;
                possibleSubTables = possibleSubTables.Where(x => x[rowIndex[iLocal]] == 5 - iLocal);
            }

            var possibleSeeds = possibleSubTables.Select(x => SubTables.ToList().IndexOf(x)).ToArray();
            return possibleSeeds;
        }

        /// <summary>
        /// Finds possible tables used to shuffle the decryption table with a known upper seed
        /// </summary>
        /// <remarks>
        /// As in <see cref="FilterPossibleShufflersWithByte2Zeros"/>, we search each possible shuffle table
        /// to find one with at least 9 rows of all zeros. The difference is this time we know which specific
        /// rows need to be all zeros, the rows containing the upper nibbles 7 through 0xf.
        /// The two shuffler IDs correspond to the positions of the bytes 0 or 0xFF in the table, so this is
        /// used to further filter the possible shufflers.
        /// </remarks>
        public void FindShuffleTable()
        {
            byte[] upperSubTable = SubTables[UpperSeed];
            byte[] inverseSubTable = CriHcaKey.InvertTable(upperSubTable);
            int[] count = new int[16];
            var possibleShufflers = new List<ShufflerId>();
            byte row0 = inverseSubTable[0];
            byte rowF = inverseSubTable[0xf];

            foreach (var s in PossibleShufflers)
            {
                // Count the number of zeros in each row
                Array.Clear(count, 0, 16);
                foreach (byte v in Byte2Zeros)
                {
                    int unshuffledUpperNibble = BackwardShuffle[s.Pos1][s.Pos2][v] >> 4;
                    count[unshuffledUpperNibble]++;
                }
                bool valid = true;

                // Invalidate the tables without the required empty rows
                for (int i = 0; i < upperSubTable.Length; i++)
                {
                    for (int j = 7; j < 15; j++)
                    {
                        if (count[inverseSubTable[j]] < 16)
                        {
                            valid = false;
                        }
                    }
                    if (count[inverseSubTable[15]] < 15)
                    {
                        valid = false;
                    }
                }

                // Invalidate the tables without the bytes 0x00 and 0xFF in their proper rows
                if ((s.Pos1 >> 4 != row0 && s.Pos1 >> 4 != rowF) ||
                    (s.Pos2 >> 4 != row0 && s.Pos2 >> 4 != rowF) ||
                    s.Pos1 >> 4 == s.Pos2 >> 4)
                {
                    valid = false;
                }

                if (valid)
                {
                    possibleShufflers.Add(s);
                }
            }
            PossibleShufflers = possibleShufflers;
        }

        public static byte[][] GenerateSubTables()
        {
            var rows = new byte[0x100][];

            for (int i = 0; i < 0x100; i++)
            {
                rows[i] = CriHcaKey.CreateRandomRow((byte)i);
            }

            return rows;
        }

        private static byte[][][] GenerateShufflers(bool backward)
        {
            var shuffler = Helpers.CreateJaggedArray<byte[][][]>(256, 256, 0);

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    shuffler[i][j] = GenerateShuffler(i, j, backward);
                }
            }

            return shuffler;
        }

        public static byte[] GenerateShuffler(int a, int b, bool backward)
        {
            byte[] table = new byte[256];
            byte x = 0;
            int outPos = 1;

            for (int i = 0; i < 256; i++)
            {
                x += 17;
                if (x != a && x != b)
                {
                    table[outPos++] = x;
                }
            }

            return backward ? table : CriHcaKey.InvertTable(table);
        }

        [DebuggerDisplay("{Pos1} - {Pos2}")]
        public class ShufflerId
        {
            public ShufflerId(int pos1, int pos2)
            {
                Pos1 = pos1;
                Pos2 = pos2;
            }

            public int Pos1 { get; }
            public int Pos2 { get; }
        }
    }
}
