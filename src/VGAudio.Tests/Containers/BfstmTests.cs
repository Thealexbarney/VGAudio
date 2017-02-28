using VGAudio.Containers;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Containers
{
    public class BfstmTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BfstmBuildAndParseEqual(int numChannels)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new BfstmWriter(), new BfstmReader());
        }
    }
}
