using VGAudio.Codecs.Atrac9;
using Xunit;

namespace VGAudio.Tests.Formats.Atrac9
{
    public class GeneratedTableTests
    {
        [Theory]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void GeneratedImdctWindowIsCorrect(int frameSize)
        {
            int tableNum = frameSize - 6;
            for (int i = 0; i < GeneratedTables.ImdctWindow[tableNum].Length; i++)
            {
                Assert.Equal(GeneratedTables.ImdctWindow[tableNum][i], Tables.ImdctWindow[tableNum][i], 13);
            }
        }

        [Theory]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void GeneratedMdctWindowIsCorrect(int frameSize)
        {
            int tableNum = frameSize - 6;
            for (int i = 0; i < GeneratedTables.MdctWindow[tableNum].Length; i++)
            {
                Assert.Equal(GeneratedTables.MdctWindow[tableNum][i], Tables.MdctWindow[tableNum][i], 13);
            }
        }

        [Fact]
        public void GeneratedSpectrumScaleIsCorrect()
        {
            Assert.Equal(GeneratedTables.SpectrumScale, Tables.SpectrumScale);
        }

        [Fact]
        public void GeneratedQuantizerStepSizeIsCorrect()
        {
            Assert.Equal(GeneratedTables.QuantizerStepSize, Tables.QuantizerStepSize);
        }

        [Fact]
        public void GeneratedQuantizerInverseStepSizeIsCorrect()
        {
            Assert.Equal(GeneratedTables.QuantizerInverseStepSize, Tables.QuantizerInverseStepSize);
        }

        [Fact]
        public void GeneratedQuantizerFineStepSizeIsCorrect()
        {
            Assert.Equal(GeneratedTables.QuantizerFineStepSize, Tables.QuantizerFineStepSize);
        }

        [Fact]
        public void UnpackedGradientCurvesIsCorrect()
        {
            Assert.Equal(GeneratedTables.GradientCurves, Tables.GradientCurves);
        }
    }
}
