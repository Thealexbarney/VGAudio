using DspAdpcm.Containers;
using DspAdpcm.Formats;
using Xunit;

namespace DspAdpcm.Tests.Containers
{
    public class BcstmTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BcstmBuildAndParseEqual(int numChannels)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new BcstmWriter(), new BcstmReader());
        }
    }
}
