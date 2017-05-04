using VGAudio.Containers;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;
using Xunit;

namespace VGAudio.Tests.Containers
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

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BcstmBuildAndParseEqualPcm16(int numChannels)
        {
            Pcm16Format audio = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new BcstmWriter { Configuration = { Codec = BxstmCodec.Pcm16Bit } };

            BuildParseTests.BuildParseCompareAudio(audio, writer, new BcstmReader());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BcstmBuildAndParseEqualPcm8(int numChannels)
        {
            Pcm8SignedFormat audio = GenerateAudio.GeneratePcm8SignedSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new BcstmWriter { Configuration = { Codec = BxstmCodec.Pcm8Bit } };

            BuildParseTests.BuildParseCompareAudio(audio, writer, new BcstmReader());
        }
    }
}
