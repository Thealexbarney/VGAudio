using DspAdpcm.Formats.GcAdpcm;
using Xunit;

namespace DspAdpcm.Tests.Helpers
{
    public class AdpcmMathTests
    {
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