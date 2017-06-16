using VGAudio.Containers.Idsp;
using VGAudio.Formats.GcAdpcm;
using Xunit;

namespace VGAudio.Tests.Containers
{
    public class IdspTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void IdspBuildAndParseEqual(int numChannels)
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new IdspWriter(), new IdspReader());
        }
    }
}
