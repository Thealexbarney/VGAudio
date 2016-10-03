using Xunit;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Tests.Helpers
{
    public class AdpcmMathTests
    {
        [Fact]
        public void SampleToNibbleConversionIsReversable()
        {
            for (int i = 1; i < 10000; i++)
            {
                int nibble = GetNibbleFromSample(i);
                int sample = GetSampleFromNibble(nibble);
                Assert.Equal(i, sample);
            }
        }
    }
}