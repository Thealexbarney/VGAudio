using VGAudio.Codecs.Atrac9;
using Xunit;

namespace VGAudio.Tests.Formats.Atrac9
{
    public class PackedTableTests
    {
        [Fact]
        public void UnpackedSfWeightsIsCorrect()
        {
            Assert.Equal(UnpackedTables.SfWeights, Tables.ScaleFactorWeights);
        }

        [Fact]
        public void UnpackedBexMode0Bands3IsCorrect()
        {
            Assert.Equal(UnpackedTables.BexMode0Bands3, Tables.BexMode0Bands3);
        }

        [Fact]
        public void UnpackedBexMode0Bands4IsCorrect()
        {
            Assert.Equal(UnpackedTables.BexMode0Bands4, Tables.BexMode0Bands4);
        }

        [Fact]
        public void UnpackedBexMode0Bands5IsCorrect()
        {
            Assert.Equal(UnpackedTables.BexMode0Bands5, Tables.BexMode0Bands5);
        }

        [Fact]
        public void UnpackedBexMode2MultIsCorrect()
        {
            Assert.Equal(UnpackedTables.BexMode2Scale, Tables.BexMode2Scale);
        }

        [Fact]
        public void UnpackedBexMode3SeedIsCorrect()
        {
            Assert.Equal(UnpackedTables.BexMode3Initial, Tables.BexMode3Initial);
        }

        [Fact]
        public void UnpackedBexMode3MultIsCorrect()
        {
            Assert.Equal(UnpackedTables.BexMode3Rate, Tables.BexMode3Rate);
        }

        [Fact]
        public void UnpackedBexMode4MultIsCorrect()
        {
            Assert.Equal(UnpackedTables.BexMode4Multiplier, Tables.BexMode4Multiplier);
        }
    }
}
