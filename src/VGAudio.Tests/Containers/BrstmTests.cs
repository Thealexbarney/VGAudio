using VGAudio.Containers;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Containers
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

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BrstmBuildAndParseEqualPcm(int numChannels)
        {
            Pcm16Format audio = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new BrstmWriter {Configuration = {Codec = BxstmCodec.Pcm16Bit}};

            BuildParseTests.BuildParseCompareAudio(audio, writer, new BrstmReader());
        }
    }
}
