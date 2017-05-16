using VGAudio.Containers;
using VGAudio.Containers.Bxstm;
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

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BfstmBuildAndParseEqualPcm16(int numChannels)
        {
            Pcm16Format audio = GenerateAudio.GeneratePcmSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new BfstmWriter { Configuration = { Codec = BxstmCodec.Pcm16Bit } };

            BuildParseTests.BuildParseCompareAudio(audio, writer, new BfstmReader());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        public void BfstmBuildAndParseEqualPcm8(int numChannels)
        {
            Pcm8SignedFormat audio = GenerateAudio.GeneratePcm8SignedSineWave(BuildParseTestOptions.Samples, numChannels, BuildParseTestOptions.SampleRate);
            var writer = new BfstmWriter { Configuration = { Codec = BxstmCodec.Pcm8Bit } };

            BuildParseTests.BuildParseCompareAudio(audio, writer, new BfstmReader());
        }

        [Fact]
        public void BfstmLoopAlignmentIsSet()
        {
            GcAdpcmFormat audio = GenerateAudio.GenerateAdpcmSineWave(BuildParseTestOptions.Samples, 1, BuildParseTestOptions.SampleRate);
            audio = audio.WithLoop(true, 1288, 16288);
            var writer = new BfstmWriter { Configuration = { LoopPointAlignment = 700 } };

            byte[] builtFile = writer.GetFile(audio);
            IAudioFormat parsedAudio = new BfstmReader().ReadFormat(builtFile);

            Assert.Equal(1400, parsedAudio.LoopStart);
            Assert.Equal(16400, parsedAudio.LoopEnd);
        }
    }
}
