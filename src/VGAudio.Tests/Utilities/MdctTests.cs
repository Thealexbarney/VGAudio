using VGAudio.Utilities;
using Xunit;

namespace VGAudio.Tests.Utilities
{
    public class MdctTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void ShuffleTablesAreCorrect(int sizeBits)
        {
            Assert.Equal(PreBuiltMdctTables.ShuffleTables[sizeBits], Mdct.GenerateShuffleTable(sizeBits));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void SinTablesAreCorrect(int sizeBits)
        {
            Mdct.GenerateTrigTables(sizeBits, out double[] sin, out _);
            for (int i = 0; i < PreBuiltMdctTables.SinTables[sizeBits].Length; i++)
            {
                Assert.Equal(PreBuiltMdctTables.SinTables[sizeBits][i], sin[i], 14);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void CosTablesAreCorrect(int sizeBits)
        {
            Mdct.GenerateTrigTables(sizeBits, out _, out double[] cos);
            for (int i = 0; i < PreBuiltMdctTables.CosTables[sizeBits].Length; i++)
            {
                Assert.Equal(PreBuiltMdctTables.CosTables[sizeBits][i], cos[i], 14);
            }
        }
    }
}
