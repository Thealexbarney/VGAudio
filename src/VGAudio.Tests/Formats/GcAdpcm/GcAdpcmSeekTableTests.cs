using System.Linq;
using Xunit;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Tests.Formats.GcAdpcm
{
    public class GcAdpcmSeekTableTests
    {
        [Theory]
        [InlineData(10, 0, false)]
        [InlineData(5, 100, true)]
        public void AssigningSeekTable(int seekTableSize, int samplesPerEntry, bool isSelfCalculated)
        {
            var seekTable = new GcAdpcmSeekTable(new short[seekTableSize], samplesPerEntry, isSelfCalculated);
            Assert.Equal(new object[] { seekTableSize, samplesPerEntry, isSelfCalculated },
                new object[] { seekTable.SeekTable.Length, seekTable.SamplesPerEntry, seekTable.IsSelfCalculated });
        }

        [Theory]
        [InlineData(1, 50, 1)]
        [InlineData(100, 50, 2)]
        [InlineData(101, 50, 3)]
        [InlineData(10000, 50, 200)]
        public void CreatingSeekTableLengthIsCorrect(int pcmLength, int samplesPerEntry, int expectedEntries)
        {
            var pcm = new short[pcmLength];
            var seekTable = new GcAdpcmSeekTable(pcm, samplesPerEntry);
            Assert.Equal(expectedEntries * 2, seekTable.SeekTable.Length);
        }

        [Fact]
        public void CreatingSeekTableContentIsCorrect()
        {
            short[] expected = { 0, 0, 50, 49, 100, 99 };
            short[] pcm = GenerateAudio.GenerateAccendingShorts(0, 101);
            var seekTable = new GcAdpcmSeekTable(pcm, 50);

            Assert.Equal(expected, seekTable.SeekTable);
        }

        [Fact]
        public void CreatingSeekTableFirstEntryIsZero()
        {
            short[] pcm = GenerateAudio.GenerateAccendingShorts(0, 50);
            var seekTable = new GcAdpcmSeekTable(pcm, 10);
            Assert.Equal(seekTable.SeekTable.Take(2), new short[] { 0, 0 });
        }
    }
}
