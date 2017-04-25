using VGAudio.Formats.GcAdpcm;
using Xunit;

namespace VGAudio.Tests.Formats.GcAdpcm
{
    public class GcAdpcmHelpersTests
    {
        [Theory]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(15, 13)]
        [InlineData(18, 14)]
        [InlineData(19, 15)]
        [InlineData(100010, 87508)]
        public void NibbleToSampleTest(int nibble, int expected)
        {
            int actual = GcAdpcmHelpers.NibbleToSample(nibble);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 3)]
        [InlineData(13, 15)]
        [InlineData(14, 18)]
        [InlineData(15, 19)]
        [InlineData(87508, 100010)]
        public void SampleToNibbleTest(int sample, int expected)
        {
            int actual = GcAdpcmHelpers.SampleToNibble(sample);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(15, 13)]
        [InlineData(16, 14)]
        [InlineData(17, 14)]
        [InlineData(18, 14)]
        [InlineData(19, 15)]
        [InlineData(100000, 87500)]
        public void NibbleCountToSampleCountTest(int nibbleCount, int expected)
        {
            int actual = GcAdpcmHelpers.NibbleCountToSampleCount(nibbleCount);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 3)]
        [InlineData(2, 4)]
        [InlineData(13, 15)]
        [InlineData(14, 16)]
        [InlineData(15, 19)]
        [InlineData(87500, 100000)]
        public void SampleCountToNibbleCountTest(int sampleCount, int expected)
        {
            int actual = GcAdpcmHelpers.SampleCountToNibbleCount(sampleCount);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SampleToNibbleConversionIsReversable()
        {
            for (int i = 1; i < 10000; i++)
            {
                int nibble = GcAdpcmHelpers.SampleToNibble(i);
                int sample = GcAdpcmHelpers.NibbleToSample(nibble);
                Assert.Equal(i, sample);
            }
        }

        [Fact]
        public void SampleCountToNibbleCountConversionIsReversable()
        {
            for (int i = 1; i < 10000; i++)
            {
                int nibbleCount = GcAdpcmHelpers.SampleCountToNibbleCount(i);
                int sampleCount = GcAdpcmHelpers.NibbleCountToSampleCount(nibbleCount);
                Assert.Equal(i, sampleCount);
            }
        }
    }
}
