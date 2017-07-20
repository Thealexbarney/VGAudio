using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VGAudio.Codecs.CriHca;

namespace VGAudio.Tools.CrackHca
{
    internal class Table
    {
        private byte[][] SubTables { get; } = Solver.GenerateSubTables();

        private byte[] UpperSubTable { get; set; }
        private byte[] Shuffler { get; set; }

        public int UpperSeed { get; set; }
        public PossibleBytes[] Seeds { get; } = new PossibleBytes[16];
        public PossibleBytes[] Cells { get; } = new PossibleBytes[256];

        private bool Changes { get; set; }

        public Table(Solver.ShufflerId shuffler, int upperSeed)
        {
            for (int i = 0; i < Seeds.Length; i++)
            {
                Seeds[i] = new PossibleBytes();
            }

            for (int i = 0; i < Cells.Length; i++)
            {
                Cells[i] = new PossibleBytes();
            }

            ApplyUpperSeed(upperSeed);
            ApplyShuffler(shuffler);

            RunCycles();
        }
        public void AddKnownValue(int encrypted, int decrypted)
        {
            int position = Shuffler[encrypted];
            Cells[position].Possible.RemoveAll(x => x != decrypted);
        }

        public void RunCycles()
        {
            do
            {
                RunCycle();
            } while (Changes);
        }

        private void RunCycle()
        {
            Changes = false;
            ApplyCellsToSeeds();
            ApplySeedsToCells();
            ApplyRelationsToSeeds();
        }

        private void ApplyUpperSeed(int seed)
        {
            UpperSeed = seed;
            UpperSubTable = SubTables[seed];

            for (int row = 0; row < 16; row++)
            {
                int row1 = row;
                for (int cell = row * 16; cell < (row + 1) * 16; cell++)
                {
                    Cells[cell].Possible.RemoveAll(x => x >> 4 != UpperSubTable[row1]);
                }
            }
        }

        /// <summary>
        /// Applies a known shuffler to the table
        /// </summary>
        /// <remarks>
        /// The two numbers of the shuffler ID correspond to the positions of the bytes 0x00 and 0xFF in the unshuffled table.
        /// Using the known upper sub-table, the positions of these two values can be determined.
        /// </remarks>
        /// <param name="shufflerId">The shuffler ID of the known shuffler</param>
        private void ApplyShuffler(Solver.ShufflerId shufflerId)
        {
            Shuffler = Solver.GenerateShuffler(shufflerId.Pos1, shufflerId.Pos2, true);
            ApplyShufflerId(shufflerId.Pos1);
            ApplyShufflerId(shufflerId.Pos2);

            void ApplyShufflerId(int position)
            {
                int row = position >> 4;
                switch (UpperSubTable[row])
                {
                    case 0:
                        Cells[position].Possible.RemoveAll(x => x != 0);
                        break;
                    case 0xf:
                        Cells[position].Possible.RemoveAll(x => x != 0xff);
                        break;
                    default:
                        throw new InvalidDataException("Incorrect position when setting shuffle table.");
                }
            }
        }

        /// <summary>
        /// Filter out impossible seeds by using the relationships between the seeds
        /// </summary>
        /// <remarks>
        /// The seeds for each row in the unshuffled table are derived from doing a very simple key expansion on the input key.
        /// For specific information on how this is done, <seealso cref="CriHcaKey.CreateDecryptionTable"/>
        /// Because each seed can be expressed in terms of any other seed, this can be used to remove impossible seeds.
        /// </remarks>
        /// 
        private void ApplyRelationsToSeeds()
        {
            ApplyXorGroup(1, 15, 0);
            ApplyXorGroup(2, 3, 6);
            ApplyXorGroup(4, 0, 3);
            ApplyXorGroup(5, 9, 6);
            ApplyXorGroup(7, 3, 6);
            ApplyXorGroup(8, 12, 9);
            ApplyXorGroup(10, 6, 9);
            ApplyXorGroup(11, 15, 12);
            ApplyXorGroup(13, 9, 12);
            ApplyXorGroup(14, 0, 15);
        }

        private void ApplyXorGroup(int a, int b, int c)
        {
            ApplyXor(a, b, c);
            ApplyXor(b, c, a);
            ApplyXor(c, a, b);
        }

        private void ApplyXor(int result, int a, int b)
        {
            Seeds[result].Possible.RemoveAll(seed =>
            {
                foreach (var seedA in Seeds[a].Possible)
                {
                    foreach (var seedB in Seeds[b].Possible)
                    {
                        if ((seedA ^ seedB) == seed)
                        {
                            return false;
                        }
                    }
                }
                Changes = true;
                return true;
            });
        }

        private void ApplySeedsToCells()
        {
            for (int cell = 0; cell < 256; cell++)
            {
                int column = cell & 0xf;
                int row = cell >> 4;
                Cells[cell].Possible.RemoveAll(value =>
                {
                    foreach (int seed in Seeds[row].Possible)
                    {
                        if (SubTables[seed][column] == (value & 0xf))
                        {
                            return false;
                        }
                    }
                    Changes = true;
                    return true;
                });
            }
        }

        private void ApplyCellsToSeeds()
        {
            for (int row = 0; row < 16; row++)
            {
                int row1 = row;
                Seeds[row].Possible.RemoveAll(seed => !IsSeedPossible(seed, row1));
            }
        }

        private bool IsSeedPossible(int seed, int row)
        {
            byte[] rand = SubTables[seed];
            for (int col = 0; col < 16; col++)
            {
                bool possible = false;
                foreach (int x in Cells[row * 16 + col].Possible)
                {
                    if ((x & 0xf) == rand[col])
                    {
                        possible = true;
                        break;
                    }
                }
                if (!possible)
                {
                    Changes = true;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Represents a list of possible bytes that a value could be.
        /// </summary>
        [DebuggerDisplay("{Possible.Count}")]
        public class PossibleBytes
        {
            private static readonly int[] Initial = Enumerable.Range(0, 0x100).ToArray();

            public List<int> Possible { get; }

            public PossibleBytes()
            {
                Possible = new List<int>(Initial);
            }
        }
    }
}
