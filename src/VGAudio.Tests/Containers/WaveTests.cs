using VGAudio.Containers;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Containers
{
    public class WaveTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void WaveBuildAndParseEqual(int numChannels)
        {
            Pcm16Format audio = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);

            BuildParseTests.BuildParseCompareAudio(audio, new WaveWriter(), new WaveReader());
        }
    }
}
