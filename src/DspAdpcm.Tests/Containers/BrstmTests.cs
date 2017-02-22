using DspAdpcm.Containers;
using DspAdpcm.Formats;
using Xunit;

namespace DspAdpcm.Tests.Containers
{
    public class BrstmTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmBuildAndParseEqual(int numChannels)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new BrstmWriter(), new BrstmReader());
        }
    }
}
