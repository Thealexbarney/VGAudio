using VGAudio.Containers;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Containers
{
    public class HpsTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void DspBuildAndParseEqual(int numChannels)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new HpsWriter(), new HpsReader());
        }
    }
}
